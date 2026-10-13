# Validate local links, fragments, and assets in a built GitHub Pages site.
require "json"
require "nokogiri"
require "optparse"
require "pathname"
require "set"
require "uri"

module DocsLinks
  def self.check_site(directory, baseurl, site_url = "https://sadicangel.github.io")
    root = File.realpath(directory)
    prefix = baseurl.gsub(%r{\A/+|/+\z}, "")
    prefix = "/#{prefix}" unless prefix.empty?
    origin = URI(site_url)
    pages = Dir.glob(File.join(root, "**/*.html")).sort.to_h do |path|
      document = Nokogiri::HTML(File.read(path, encoding: "UTF-8"))
      ids = document.css("[id], a[name]").flat_map { |node| [node["id"], node["name"]].compact }.to_set
      links = document.css("[href], [src], option[value]").flat_map do |node|
        values = [node["href"], node["src"]]
        values << node["value"] if node.name == "option" && node["value"].start_with?("/")
        values.compact.reject(&:empty?)
      end
      [File.realpath(path), { ids: ids, links: links }]
    end
    return ["No HTML pages found in #{root}"] if pages.empty?

    errors = []
    pages.each do |path, page|
      relative = Pathname.new(path).relative_path_from(Pathname.new(root)).to_s.tr("\\", "/")
      source_url = "#{site_url.chomp('/')}#{prefix}/#{relative}"
      page[:links].each do |link|
        label = "#{relative}: #{link}"
        begin
          url = URI.join(source_url, link)
        rescue URI::InvalidURIError => error
          errors << "#{label}: invalid URL: #{error.message}"
          next
        end
        next unless %w[http https].include?(url.scheme) && url.host == origin.host && url.port == origin.port

        decoded_path = URI::DEFAULT_PARSER.unescape(url.path)
        unless prefix.empty? || decoded_path == prefix || decoded_path.start_with?(prefix + "/")
          errors << "#{label}: missing base path #{prefix}"
          next
        end
        target = File.expand_path(decoded_path.delete_prefix(prefix).sub(%r{\A/+}, ""), root)
        target = File.realpath(target) if File.exist?(target)
        unless target == root || target.start_with?(root + File::SEPARATOR)
          errors << "#{label}: path escapes site root"
          next
        end
        target = File.join(target, "index.html") if File.directory?(target)
        unless File.file?(target)
          errors << "#{label}: target does not exist"
          next
        end
        fragment = URI::DEFAULT_PARSER.unescape(url.fragment.to_s)
        if !fragment.empty? && File.extname(target) == ".html" && !pages.fetch(File.realpath(target))[:ids].include?(fragment)
          errors << "#{label}: fragment ##{fragment} does not exist"
        end
      end
    end
    Dir.glob(File.join(root, "**/search-data.json")).sort.each do |index|
      relative = Pathname.new(index).relative_path_from(Pathname.new(root))
      version_path = relative.parent.parent.parent.to_s.tr("\\", "/")
      version_prefix = prefix + (version_path == "." ? "" : "/#{version_path}") + "/"
      begin
        entries = JSON.parse(File.read(index, encoding: "UTF-8"))
        raise TypeError, "expected an object" unless entries.is_a?(Hash)

        entries.each_value do |entry|
          url = entry.fetch("url")
          raise TypeError, "expected a URL string" unless url.is_a?(String)

          errors << "#{relative}: search result escapes its version: #{url}" unless url.start_with?(version_prefix)
        end
      rescue JSON::ParserError, KeyError, TypeError, NoMethodError => error
        errors << "#{relative}: invalid search index: #{error.message}"
      end
    end
    errors
  end
end

if $PROGRAM_NAME == __FILE__
  settings = { baseurl: "/avro-source-generator", site_url: "https://sadicangel.github.io" }
  parser = OptionParser.new do |options|
    options.banner = "Usage: ruby check_links.rb DIRECTORY [options]"
    options.on("--baseurl PATH") { |value| settings[:baseurl] = value }
    options.on("--site-url URL") { |value| settings[:site_url] = value }
  end
  parser.parse!
  abort parser.to_s unless ARGV.length == 1

  errors = DocsLinks.check_site(ARGV.first, settings[:baseurl], settings[:site_url])
  unless errors.empty?
    puts errors
    exit 1
  end
  puts "All internal links, fragments, and assets are valid in #{ARGV.first}."
end
