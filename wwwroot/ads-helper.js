window.loadAdSense = () => {
    const script = document.createElement("script");
    script.src =
      "https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=ca-pub-3629979359038521";
    script.async = true;
    script.crossOrigin = "anonymous";
    script.onload = () => {
      (adsbygoogle = window.adsbygoogle || []).push({});
    };

    document.head.appendChild(script);
};