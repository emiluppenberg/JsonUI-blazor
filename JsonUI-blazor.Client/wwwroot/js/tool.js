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

window.registerClickOutside = (element, dotNetRef) => {
  const handler = (e) => {
    if (!element.contains(e.target) && e.target.id !== "annotation") {
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

window.initializeAds_tool = async () => {
  const consent = localStorage.getItem("cookie-consent");

  if (consent !== "accepted") return;

  const num = window._isMobile ? 1 : 2;
  console.log(window._isMobile);

  await Promise.resolve(
    setTimeout(() => {
      const tool_ad = document.getElementById("tool-ad");

      if (tool_ad) {
        window.atOptions = {
          key: window._bannerKey,
          format: "iframe",
          height: window._bannerH,
          width: window._bannerW,
          params: {},
        };

        for (let i = 0; i < num; i++) {
          const script = document.createElement("script");
          script.src = window._bannerSrc;
          script.async = true;
          tool_ad.appendChild(script);
        }
      }
    }, 50),
  );
};