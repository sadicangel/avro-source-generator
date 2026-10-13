(() => {
  "use strict";

  const storageKey = "avro-source-generator-theme";
  const validThemes = ["light", "dark"];
  const systemTheme = window.matchMedia("(prefers-color-scheme: dark)");
  let preference = "system";
  try {
    const saved = localStorage.getItem(storageKey);
    if (validThemes.includes(saved)) preference = saved;
  } catch {
    // System mode remains usable when storage is unavailable.
  }

  function applyTheme(theme) {
    document.documentElement.dataset.docsTheme = theme;
    document.documentElement.style.colorScheme = theme === "system" ? "light dark" : theme;
    const stylesheet = document.getElementById("docs-dark-theme");
    stylesheet.media = theme === "system" ? "(prefers-color-scheme: dark)" : theme === "dark" ? "all" : "not all";
  }

  applyTheme(preference);
  document.addEventListener("DOMContentLoaded", () => {
    const toggle = document.getElementById("docs-theme-toggle");
    const isDark = () => preference === "system" ? systemTheme.matches : preference === "dark";
    function updateToggle() {
      toggle.setAttribute("aria-checked", String(isDark()));
      toggle.title = isDark() ? "Switch to light theme" : "Switch to dark theme";
    }
    updateToggle();
    toggle.hidden = false;
    toggle.addEventListener("click", () => {
      preference = isDark() ? "light" : "dark";
      applyTheme(preference);
      updateToggle();
      try { localStorage.setItem(storageKey, preference); } catch { /* Keep the in-page preference. */ }
    });
    systemTheme.addEventListener("change", () => {
      if (preference === "system") updateToggle();
    });

    document.getElementById("docs-version").addEventListener("change", (event) => {
      location.assign(event.target.value);
    });
  });
})();
