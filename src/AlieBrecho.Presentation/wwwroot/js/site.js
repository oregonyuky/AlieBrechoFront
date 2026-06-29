const navbar = document.getElementById("navbar");
const topRibbon = document.getElementById("topRibbon");

function syncNavState() {
  const scrolled = window.scrollY > 0;
  navbar?.classList.toggle("scrolled", scrolled);
  navbar?.classList.toggle("ribbon-hidden", scrolled);
  topRibbon?.classList.toggle("hidden", scrolled);
}

window.addEventListener("scroll", syncNavState, { passive: true });
syncNavState();

const hamburger = document.getElementById("hamburger");
const mobileMenu = document.getElementById("mobileMenu");
const mobileClose = document.getElementById("mobileClose");
const mobileOverlay = document.getElementById("mobileOverlay");

function openMenu() {
  hamburger?.classList.add("open");
  mobileMenu?.classList.add("open");
  mobileOverlay?.classList.add("open");
  document.body.style.overflow = "hidden";
}

function closeMenu() {
  hamburger?.classList.remove("open");
  mobileMenu?.classList.remove("open");
  mobileOverlay?.classList.remove("open");
  document.body.style.overflow = "";
}

hamburger?.addEventListener("click", openMenu);
mobileClose?.addEventListener("click", closeMenu);
mobileOverlay?.addEventListener("click", closeMenu);
mobileMenu?.querySelectorAll("a").forEach((link) => {
  link.addEventListener("click", closeMenu);
});

const revealElements = document.querySelectorAll(".reveal");
if ("IntersectionObserver" in window) {
  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        entry.target.classList.add("visible");
      }
    });
  }, { threshold: 0.12 });

  revealElements.forEach((element) => observer.observe(element));
} else {
  revealElements.forEach((element) => element.classList.add("visible"));
}

const toast = document.getElementById("toast");

function showToast(message) {
  if (!toast) {
    return;
  }

  toast.textContent = message;
  toast.classList.add("show");
  window.setTimeout(() => toast.classList.remove("show"), 2600);
}

document.getElementById("newsletterBtn")?.addEventListener("click", () => {
  showToast("Obrigada. Voce vai receber os proximos drops.");
});

document.querySelectorAll(".tab-btn[data-tab]").forEach((button) => {
  button.addEventListener("click", () => {
    const tab = button.dataset.tab;
    document.querySelectorAll(".tab-btn[data-tab]").forEach((item) => {
      item.classList.toggle("active", item === button);
    });
    document.querySelectorAll(".tab-panel").forEach((panel) => {
      panel.classList.toggle("active", panel.id === `tab-${tab}`);
    });
  });
});

document.querySelectorAll(".share-btn[data-share]").forEach((button) => {
  button.addEventListener("click", () => {
    const title = document.title;
    const url = window.location.href;
    const share = button.dataset.share;

    if (share === "whatsapp") {
      window.open(`https://wa.me/?text=${encodeURIComponent(`${title} ${url}`)}`, "_blank", "noopener");
      return;
    }

    if (share === "pinterest") {
      window.open(`https://www.pinterest.com/pin/create/button/?url=${encodeURIComponent(url)}`, "_blank", "noopener");
      return;
    }

    showToast("Copie o link e compartilhe no Instagram.");
  });
});

document.getElementById("copyBtn")?.addEventListener("click", async () => {
  try {
    await navigator.clipboard.writeText(window.location.href);
    showToast("Link do produto copiado.");
  } catch {
    showToast("Nao foi possivel copiar o link.");
  }
});
