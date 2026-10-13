require "fileutils"
require "minitest/autorun"
require "tmpdir"
require_relative "check_links"

class LinkChecks < Minitest::Test
  def setup
    @temporary_root = File.realpath(Dir.tmpdir)
    @root = Dir.mktmpdir("docs-links-", @temporary_root)
  end

  def teardown
    resolved = File.realpath(@root)
    raise "Unexpected temporary directory" unless resolved.start_with?(@temporary_root + File::SEPARATOR)

    FileUtils.remove_entry(resolved)
  end

  def write(name, text)
    path = File.join(@root, name)
    FileUtils.mkdir_p(File.dirname(path))
    File.write(path, text, encoding: "UTF-8")
  end

  def test_checks_relative_prefixed_and_same_origin_links
    write("nested/index.html", '<h1 id="hello world">Hi</h1><a href="../index.html">Home</a>' \
      '<a href="https://sadicangel.github.io/guide/#top">Top</a>')
    write("index.html", '<h1 id="top">Home</h1><a href="/guide/nested/#hello%20world">Nested</a>')
    assert_empty DocsLinks.check_site(@root, "/guide")
  end

  def test_reports_missing_target_and_anchor
    write("index.html", '<a href="missing/">Missing</a><a href="#absent">Absent</a>')
    errors = DocsLinks.check_site(@root, "/guide")
    assert_equal 2, errors.length
    assert errors.any? { |error| error.include?("target does not exist") }
    assert errors.any? { |error| error.include?("fragment #absent") }
  end

  def test_requires_repository_base_path
    write("index.html", '<img src="/assets/icon.png">')
    assert_includes DocsLinks.check_site(@root, "/guide").first, "missing base path /guide"
  end

  def test_checks_assets_and_ignores_external_links
    write("index.html", '<img src="/guide/icon.png?v=1">' \
      '<a href="https://example.org/missing">External</a><a href="mailto:a@example.org">Mail</a>')
    write("icon.png", "placeholder")
    assert_empty DocsLinks.check_site(@root, "/guide")
  end

  def test_rejects_encoded_path_escape
    write("index.html", '<a href="/guide/%2e%2e/outside.html">Escape</a>')
    assert_includes DocsLinks.check_site(@root, "/guide").first, "path escapes site root"
  end

  def test_empty_build_fails
    assert_includes DocsLinks.check_site(@root, "/guide").first, "No HTML pages"
  end

  def test_checks_version_selector_destinations
    write("index.html", '<select><option value="/guide/missing/">Version</option></select>')
    assert_includes DocsLinks.check_site(@root, "/guide").first, "target does not exist"
  end

  def test_search_results_stay_within_their_version
    write("index.html", "<p>Home</p>")
    write("v1/assets/js/search-data.json", '{"0":{"url":"/guide/v2/configuration/"}}')
    assert_includes DocsLinks.check_site(@root, "/guide").first, "search result escapes its version"
  end

  def test_parses_entities_and_ignores_markup_inside_scripts
    write("index.html", '<h1 id="a&amp;b">Heading</h1><a href="#a&amp;b">Heading</a>' \
      '<script>const example = \'<a href="missing/">\';</script>')
    assert_empty DocsLinks.check_site(@root, "/guide")
  end
end
