# Build each documentation version independently so navigation and search stay scoped.
require "fileutils"
require "json"
require "optparse"
require "tmpdir"
require "yaml"

repository = File.expand_path("../..", __dir__)
artifacts = File.join(repository, "artifacts")
destination = File.join(artifacts, "docs-site")
serve = false
OptionParser.new do |options|
  options.on("--destination PATH") { |path| destination = File.expand_path(path) }
  options.on("--serve") { serve = true }
end.parse!

# All recursive cleanup is restricted to a resolved subdirectory of artifacts.
FileUtils.mkdir_p(artifacts)
artifacts = File.realpath(artifacts)
resolved_destination = File.exist?(destination) ? File.realpath(destination) : File.expand_path(destination)
unless resolved_destination.start_with?(artifacts + File::SEPARATOR)
  abort "Documentation output must be a subdirectory of #{artifacts}"
end
FileUtils.rm_rf(resolved_destination)
FileUtils.mkdir_p(resolved_destination)

docs = File.join(repository, "docs")
settings = YAML.safe_load_file(File.join(docs, "_config.yml"))
versions = JSON.parse(File.read(File.join(docs, "_data/versions.json")))
versions.each do |version|
  unless version.fetch("id").match?(/\A(?:\d+\.\d+\.\d+|preview)\z/)
    abort "Invalid documentation version: #{version['id']}"
  end
  version["topics"] = Dir.glob(File.join(docs, "versions", version["id"], "*.md")).map do |page|
    File.basename(page, ".md")
  end
  abort "Missing home page for #{version['id']}" unless version["topics"].include?("index")
end

versions.each do |version|
  Dir.mktmpdir("docs-build-", artifacts) do |source|
    %w[_includes _layouts assets].each do |entry|
      FileUtils.cp_r(File.join(docs, entry), source)
    end
    FileUtils.cp_r(Dir.glob(File.join(docs, "versions", version["id"], "*")), source)
    config = settings.merge(
      "repository_baseurl" => settings.fetch("baseurl"),
      "baseurl" => "#{settings.fetch('baseurl')}/#{version['id']}",
      "docs_version" => version,
      "docs_versions" => versions,
      "gh_edit_source" => "docs/versions/#{version['id']}"
    )
    File.write(File.join(source, "_config.yml"), config.to_yaml)
    puts "Building #{version['label']} (#{version['channel']})"
    success = system(Gem.ruby, Gem.bin_path("jekyll", "jekyll"), "build",
                     "--source", source, "--destination", File.join(resolved_destination, version["id"]), "--trace")
    abort "Documentation build failed for #{version['id']}" unless success
  end
end

stable = versions.find { |version| version["channel"] == "Stable" }
abort "No default stable version configured" unless stable
redirects = { "" => "", "serialization" => "kafka" }
stable["topics"].each { |topic| redirects[topic] = topic unless topic == "index" }
redirects.each do |old_topic, new_topic|
  target = "#{settings.fetch('baseurl')}/#{stable['id']}/"
  target += "#{new_topic}/" unless new_topic.empty?
  directory = File.join(resolved_destination, old_topic)
  FileUtils.mkdir_p(directory)
  File.write(File.join(directory, "index.html"), <<~HTML)
    <!doctype html>
    <html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
    <title>Avro Source Generator documentation</title><link rel="canonical" href="#{settings.fetch('url')}#{target}">
    <meta http-equiv="refresh" content="0;url=#{target}">
    <script>location.replace(#{JSON.generate(target)} + location.hash);</script></head>
    <body><p><a href="#{target}">Open the Avro Source Generator guide</a>.</p></body></html>
  HTML
end
puts "Documentation built at #{resolved_destination}"

if serve
  require "webrick"
  server = WEBrick::HTTPServer.new(Port: 4000, BindAddress: "127.0.0.1")
  server.mount(settings.fetch("baseurl"), WEBrick::HTTPServlet::FileHandler, resolved_destination)
  trap("INT") { server.shutdown }
  puts "Preview: http://127.0.0.1:4000#{settings.fetch('baseurl')}/"
  server.start
end
