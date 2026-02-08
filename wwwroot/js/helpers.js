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

window.initCookieConsent = () => {
  const banner = document.querySelector("[data-cookie-consent]");

  if (!banner) {
    return;
  }

  const storageKey = banner.dataset.storageKey || "cookie-consent";

  const emitConsent = (value) => {
    window.dispatchEvent(
      new CustomEvent("cookie-consent-changed", { detail: value }),
    );
  };

  const showBanner = () => {
    banner.classList.remove("scale-0");
    banner.classList.add("scale-100");
  };

  const hideBanner = () => {
    banner.classList.remove("scale-100");
    banner.classList.add("scale-0");
  };

  if (banner.dataset.handlersBound !== "true") {
    banner.querySelectorAll("[data-cookie-action]").forEach((button) => {
      button.addEventListener("click", () => {
        const action = button.dataset.cookieAction;

        if (!action) {
          return;
        }

        localStorage.setItem(storageKey, action);
        hideBanner();
        emitConsent(action);
      });
    });

    banner.dataset.handlersBound = "true";
  }

  const consent = localStorage.getItem(storageKey);

  if (consent === "accepted" || consent === "rejected") {
    hideBanner();
    emitConsent(consent);
    return;
  }

  setTimeout(showBanner, 500);
};

window.initHomeConsentAd = () => {
  const container = document.querySelector("[data-home-ad-container]");

  if (!container) {
    return;
  }

  const storageKey = container.dataset.storageKey || "cookie-consent";

  const applyConsent = (consent) => {
    if (consent === "accepted") {
      container.style.display = "";

      if (container.dataset.adLoaded !== "true") {
        window.loadAdSense();
        container.dataset.adLoaded = "true";
      }

      return;
    }

    container.style.display = "none";
  };

  applyConsent(localStorage.getItem(storageKey));

  if (container.dataset.listenerBound === "true") {
    return;
  }

  window.addEventListener("cookie-consent-changed", (event) => {
    applyConsent(event.detail);
  });

  container.dataset.listenerBound = "true";
};

window.initConsentUi = () => {
  window.initCookieConsent();
  window.initHomeConsentAd();
};

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", () => {
    window.initConsentUi();
  });
} else {
  window.initConsentUi();
}

window.addEventListener("enhancedload", () => {
  window.initConsentUi();
});

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
