window._isMobile = window.matchMedia("(max-width: 767px)").matches;

window._toolAdSlots = {
  desktop: [
    {
      containerId: "tool-ad-desktop-1",
      key: "ca82a623b5aee2f099b48fef189e5c09",
      width: 728,
      height: 90,
    },
    {
      containerId: "tool-ad-desktop-2",
      key: "9b6c2cfdbbcfaaaeb6849de82d0c1249",
      width: 728,
      height: 90,
    },
  ],
  mobile: [
    {
      containerId: "tool-ad-mobile-1",
      key: "1a7b6ee059605baf5f741848a58df24e",
      width: 320,
      height: 50,
    },
  ],
};

window._homeAdSlots = {
  desktop: [
    {
      containerId: "home-ad-desktop-1",
      key: "ca82a623b5aee2f099b48fef189e5c09",
      width: 728,
      height: 90,
    },
    {
      containerId: "home-ad-desktop-2",
      key: "9b6c2cfdbbcfaaaeb6849de82d0c1249",
      width: 728,
      height: 90,
    },
  ],
  mobile: [
    {
      containerId: "home-ad-mobile-1",
      key: "1a7b6ee059605baf5f741848a58df24e",
      width: 320,
      height: 50,
    },
  ],
};

window.loadAdSlot = (slot) =>
  new Promise((resolve) => {
    const container = document.getElementById(slot.containerId);

    if (!container) {
      resolve(false);
      return;
    }

    window.atOptions = {
      key: slot.key,
      format: "iframe",
      height: slot.height,
      width: slot.width,
      params: {},
    };

    const script = document.createElement("script");
    script.src = `https://www.highperformanceformat.com/${slot.key}/invoke.js`;
    script.async = true;

    script.onload = () => {
      resolve(true);
    };

    script.onerror = () => resolve(false);
    container.appendChild(script);
  });

window.initializeAds = async (page) => {
  const consent = localStorage.getItem("cookie-consent");
  if (consent !== "accepted") return;

  let pageSlots = {};
  if (page === "home") pageSlots = window._homeAdSlots;
  if (page === "tool") pageSlots = window._toolAdSlots;

  const slots = window._isMobile
    ? pageSlots.mobile
    : pageSlots.desktop;

  for (const slot of slots) {
    await window.loadAdSlot(slot);
  }
};