window.createStickyObserver = (sentinel, dotNetRef) => {
  const observer = new IntersectionObserver(
    ([entry]) => {
      const appContainer = document.getElementById("app-container");
      const appStyle = getComputedStyle(appContainer);
      const appPadding = parseFloat(appStyle.paddingLeft);

      const nodeContainer = document.getElementById("node-container");
      const nodeStyle = getComputedStyle(nodeContainer);
      const nodePadding = parseFloat(nodeStyle.paddingLeft);

      const jsonContainer = document.getElementById("json-container");
      const jsonRect = jsonContainer.getBoundingClientRect();

      const stickyRectLeft =
        entry.boundingClientRect.left + appPadding + nodePadding;

      if (stickyRectLeft >= jsonRect.right) {
        return;
      }

      dotNetRef.invokeMethodAsync(
        "OnStickyStateChanged",
        !entry.isIntersecting,
      );
    },
    { threshold: 1 },
  );

  observer.observe(sentinel);

  return {
    disconnect: () => observer.disconnect(),
  };
};

window.scrollToElement = (element) => {
  const container = document.getElementById("node-container");
  const cRect = container.getBoundingClientRect();
  const eRect = element.getBoundingClientRect();

  const scrollTo = eRect.top - cRect.top + container.scrollTop + 1;

  container.scrollTo({ top: scrollTo, behavior: "smooth" });
};

window.loadAdSense = () => {
  if (!window.__adsenseLoaded) {
    window.__adsenseLoaded = true;

    const script = document.createElement("script");
    script.src =
      "https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=ca-pub-3629979359038521";
    script.async = true;
    script.crossOrigin = "anonymous";
    script.onload = () => {
      (adsbygoogle = window.adsbygoogle || []).push({});
    };

    document.head.appendChild(script);
    return;
  }

  (adsbygoogle = window.adsbygoogle || []).push({});
};

window.registerClickOutside = (element, dotNetRef) => {
  const handler = (e) => {
    if (!element.contains(e.target)) {
      console.log(e.target);
      dotNetRef.invokeMethodAsync("OnClickOutside");
    }
  };

  window.addEventListener("click", handler);

  element._outsideHandler = handler;
};

window.unregisterClickOutside = (element) => {
  if (element._outsideHandler) {
    window.removeEventListener("click", element._outsideHandler);
  }
};
