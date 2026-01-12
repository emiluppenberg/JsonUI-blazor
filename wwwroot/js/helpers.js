window.createStickyObserver = (sentinel, dotNetRef) => {
  const observer = new IntersectionObserver(
    ([entry]) => {
      dotNetRef.invokeMethodAsync(
        "OnStickyStateChanged",
        !entry.isIntersecting
      );
    },
    { threshold: 1 }
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
  (adsbygoogle = window.adsbygoogle || []).push({});
};
