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
  document.body.style.overflow = cartDrawer?.classList.contains("open") ? "hidden" : "";
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

const cartDrawer = document.getElementById("cartDrawer");
const cartOverlay = document.getElementById("cartOverlay");
const cartBtn = document.getElementById("cartBtn");
const cartClose = document.getElementById("cartClose");
const cartBody = document.getElementById("cartBody");
const cartEmpty = document.getElementById("cartEmpty");
const cartTotal = document.getElementById("cartTotal");
const cartBadge = document.getElementById("cartBadge");
const cepInput = document.getElementById("cepInput");
const calcFreteBtn = document.getElementById("calcFreteBtn");
const freteError = document.getElementById("freteError");
const freteResults = document.getElementById("freteResults");
const pacPrazo = document.getElementById("pacPrazo");
const pacPrice = document.getElementById("pacPrice");
const sedexPrazo = document.getElementById("sedexPrazo");
const sedexPrice = document.getElementById("sedexPrice");
const token = document.querySelector("meta[name='request-verification-token']")?.content;
let currentCartSubtotal = 0;

function openCart() {
  cartDrawer?.classList.add("open");
  cartDrawer?.setAttribute("aria-hidden", "false");
  cartOverlay?.classList.add("open");
  document.body.style.overflow = "hidden";
}

function closeCart() {
  cartDrawer?.classList.remove("open");
  cartDrawer?.setAttribute("aria-hidden", "true");
  cartOverlay?.classList.remove("open");
  document.body.style.overflow = mobileMenu?.classList.contains("open") ? "hidden" : "";
}

function cartHeaders() {
  const headers = {
    Accept: "application/json",
    "X-Requested-With": "XMLHttpRequest"
  };

  if (token) {
    headers.RequestVerificationToken = token;
  }

  return headers;
}

function escapeHtml(value) {
  const div = document.createElement("div");
  div.textContent = value ?? "";
  return div.innerHTML;
}

function renderCart(cart) {
  currentCartSubtotal = Number(cart?.subtotal ?? 0);

  if (cartTotal) {
    cartTotal.textContent = cart?.subtotalText ?? "R$ 0,00";
  }

  if (cartBadge) {
    cartBadge.textContent = cart?.itemCount ?? 0;
  }

  if (!cartBody || !cartEmpty) {
    return;
  }

  if (!cart?.items?.length) {
    cartBody.innerHTML = "";
    cartBody.appendChild(cartEmpty);
    if (freteResults?.classList.contains("show")) {
      calculateShipping();
    }
    return;
  }

  cartBody.innerHTML = "";
  cart.items.forEach((item) => {
    const row = document.createElement("div");
    row.className = "cart-item";
    const name = escapeHtml(item.name);
    const imageUrl = escapeHtml(item.imageUrl);
    const size = item.size ? `Tamanho: ${escapeHtml(item.size)}` : "Tamanho unico";

    const media = item.imageUrl
      ? `<img src="${imageUrl}" alt="${name}">`
      : `<span>${name}</span>`;

    row.innerHTML = `
      <div class="cart-item__img">${media}</div>
      <div class="cart-item__info">
        <div class="cart-item__name">${name}</div>
        <div class="cart-item__size">${size}</div>
        <div class="cart-item__qty-row">
          <span class="qty-val">${item.quantity} unidade</span>
        </div>
        <button class="cart-item__rm" type="button" data-action="remove" data-product-id="${escapeHtml(item.id)}">Remover</button>
      </div>
      <div class="cart-item__price">${escapeHtml(item.totalText)}</div>
    `;

    cartBody.appendChild(row);
  });

  if (freteResults?.classList.contains("show")) {
    calculateShipping();
  }
}

function formatCep(value) {
  const digits = value.replace(/\D/g, "").slice(0, 8);
  if (digits.length <= 5) {
    return digits;
  }

  return `${digits.slice(0, 5)}-${digits.slice(5)}`;
}

function formatCurrency(value) {
  return value.toLocaleString("pt-BR", {
    style: "currency",
    currency: "BRL"
  });
}

function calculateShipping() {
  const digits = cepInput?.value.replace(/\D/g, "") ?? "";
  const valid = digits.length === 8;

  freteError?.classList.toggle("show", !valid);
  freteResults?.classList.toggle("show", valid);

  if (!valid) {
    return;
  }

  const regionSeed = Number(digits.slice(0, 2));
  const pacDays = 4 + (regionSeed % 4);
  const sedexDays = 1 + (regionSeed % 3);
  const pacValue = currentCartSubtotal >= 150 ? 0 : 14.9 + (regionSeed % 5) * 1.8;
  const sedexValue = 24.9 + (regionSeed % 6) * 2.4;

  if (pacPrazo) {
    pacPrazo.textContent = `Entrega em ${pacDays} a ${pacDays + 2} dias uteis`;
  }

  if (sedexPrazo) {
    sedexPrazo.textContent = `Entrega em ${sedexDays} a ${sedexDays + 1} dias uteis`;
  }

  if (pacPrice) {
    pacPrice.textContent = pacValue === 0 ? "Gratis" : formatCurrency(pacValue);
    pacPrice.classList.toggle("gratis", pacValue === 0);
  }

  if (sedexPrice) {
    sedexPrice.textContent = formatCurrency(sedexValue);
  }
}

async function fetchCart() {
  const response = await fetch("/Cart?handler=Summary", {
    headers: { Accept: "application/json" }
  });

  if (!response.ok) {
    throw new Error("Nao foi possivel carregar o carrinho.");
  }

  const cart = await response.json();
  renderCart(cart);
  return cart;
}

async function postCart(action, productId) {
  const formData = new FormData();
  formData.append("productId", productId);

  const response = await fetch(`/Cart?handler=${action}`, {
    method: "POST",
    headers: cartHeaders(),
    body: formData
  });

  if (!response.ok) {
    throw new Error("Nao foi possivel atualizar o carrinho.");
  }

  const cart = await response.json();
  renderCart(cart);
  return cart;
}

cartBtn?.addEventListener("click", async () => {
  try {
    await fetchCart();
    openCart();
  } catch {
    window.location.href = "/Cart";
  }
});

cartClose?.addEventListener("click", closeCart);
cartOverlay?.addEventListener("click", closeCart);

cepInput?.addEventListener("input", () => {
  cepInput.value = formatCep(cepInput.value);
  freteError?.classList.remove("show");
  freteResults?.classList.remove("show");
});

cepInput?.addEventListener("keydown", (event) => {
  if (event.key === "Enter") {
    event.preventDefault();
    calculateShipping();
  }
});

calcFreteBtn?.addEventListener("click", calculateShipping);

cartBody?.addEventListener("click", async (event) => {
  const button = event.target.closest("[data-action][data-product-id]");
  if (!button) {
    return;
  }

  try {
    await postCart(button.dataset.action, button.dataset.productId);
  } catch {
    showToast("Nao foi possivel atualizar o carrinho.");
  }
});

document.querySelectorAll("[data-cart-form]").forEach((form) => {
  form.addEventListener("submit", async (event) => {
    event.preventDefault();

    try {
      const response = await fetch(form.action, {
        method: "POST",
        headers: cartHeaders(),
        body: new FormData(form)
      });

      if (!response.ok) {
        throw new Error("Nao foi possivel adicionar ao carrinho.");
      }

      const cart = await response.json();
      renderCart(cart);
      openCart();
      showToast("Produto adicionado ao carrinho.");
    } catch {
      form.submit();
    }
  });
});

document.addEventListener("keydown", (event) => {
  if (event.key === "Escape" && cartDrawer?.classList.contains("open")) {
    closeCart();
  }
});

fetchCart().catch(() => {
  renderCart({ itemCount: 0, subtotalText: "R$ 0,00", items: [] });
});

function initCheckoutDynamics() {
  const form = document.getElementById("checkoutForm");
  if (!form) {
    return;
  }

  let currentStep = 1;
  let fretePrice = 0;
  let cupomDiscount = 0;
  const basePrice = Number.parseFloat(form.dataset.subtotal || "0") || 0;
  const cupons = { ALIE10: 10, BRECHO15: 15, BEMVINDA: 20 };

  const byId = (id) => document.getElementById(id);
  const val = (id) => byId(id)?.value || "";
  const money = (value) => value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });

  function applyMask(id, fn) {
    const input = byId(id);
    input?.addEventListener("input", () => {
      input.value = fn(input.value);
    });
  }

  applyMask("telefone", (value) => {
    const digits = value.replace(/\D/g, "").slice(0, 11);
    if (digits.length > 10) {
      return digits.replace(/(\d{2})(\d{5})(\d{4})/, "($1) $2-$3");
    }
    if (digits.length > 6) {
      return digits.replace(/(\d{2})(\d{4})(\d{0,4})/, "($1) $2-$3");
    }
    if (digits.length > 2) {
      return digits.replace(/(\d{2})(\d{0,5})/, "($1) $2");
    }
    return digits;
  });

  applyMask("cpf", (value) => {
    const digits = value.replace(/\D/g, "").slice(0, 11);
    if (digits.length > 9) {
      return digits.replace(/(\d{3})(\d{3})(\d{3})(\d{0,2})/, "$1.$2.$3-$4");
    }
    if (digits.length > 6) {
      return digits.replace(/(\d{3})(\d{3})(\d{0,3})/, "$1.$2.$3");
    }
    if (digits.length > 3) {
      return digits.replace(/(\d{3})(\d{0,3})/, "$1.$2");
    }
    return digits;
  });

  applyMask("cep", (value) => {
    const digits = value.replace(/\D/g, "").slice(0, 8);
    return digits.length > 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits;
  });

  function markErr(id, message) {
    const field = byId(`field-${id}`);
    if (!field) {
      return;
    }

    field.classList.add("field--error");
    const error = field.querySelector(".field__error");
    if (error && message) {
      error.textContent = message;
    }
  }

  function clearErr(id) {
    byId(`field-${id}`)?.classList.remove("field--error");
  }

  function scrollToCard(step) {
    window.setTimeout(() => {
      byId(`card-${step}`)?.scrollIntoView({ behavior: "smooth", block: "start" });
    }, 120);
  }

  function setProgress(step) {
    for (let index = 1; index <= 4; index += 1) {
      const progress = byId(`prog-${index}`);
      progress?.classList.remove("active", "done");
      if (index < step) {
        progress?.classList.add("done");
      } else if (index === step) {
        progress?.classList.add("active");
      }
    }
  }

  function openStep(step) {
    for (let index = 1; index <= 4; index += 1) {
      byId(`card-${index}`)?.classList.remove("active");
    }

    const card = byId(`card-${step}`);
    card?.classList.add("active");
    card?.classList.remove("done");
    currentStep = step;
    setProgress(step);
    scrollToCard(step);
  }

  function doneStep(step, summaryText) {
    const card = byId(`card-${step}`);
    const summary = byId(`sum-${step}`);
    const number = byId(`num-${step}`);

    card?.classList.remove("active");
    card?.classList.add("done");
    if (summary) {
      summary.textContent = summaryText;
    }
    if (number) {
      number.textContent = "OK";
    }
  }

  function editStep(step) {
    for (let index = step; index <= 4; index += 1) {
      byId(`card-${index}`)?.classList.remove("done", "active");
      const number = byId(`num-${index}`);
      if (number) {
        number.textContent = index;
      }
    }
    openStep(step);
  }

  function recalcTotal() {
    const total = Math.max(0, basePrice + fretePrice - cupomDiscount);
    const pixValue = total * 0.97;

    if (byId("val-total")) {
      byId("val-total").textContent = money(total);
    }
    if (byId("pixSummary")) {
      byId("pixSummary").textContent = money(pixValue);
    }
    if (byId("pixPrice")) {
      byId("pixPrice").textContent = money(pixValue);
    }
  }

  function confirmEmail() {
    const email = val("email").trim();
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      markErr("email", "Digite um e-mail valido");
      return false;
    }

    clearErr("email");
    doneStep(1, email);
    openStep(2);
    return true;
  }

  function confirmDados() {
    let ok = true;
    const nome = val("nome").trim();
    const sobrenome = val("sobrenome").trim();
    const cpf = val("cpf").replace(/\D/g, "");
    const telefone = val("telefone").replace(/\D/g, "");

    if (!nome) {
      markErr("nome", "Nome obrigatorio");
      ok = false;
    } else {
      clearErr("nome");
    }

    if (!sobrenome) {
      markErr("sobrenome", "Sobrenome obrigatorio");
      ok = false;
    } else {
      clearErr("sobrenome");
    }

    if (cpf.length !== 11) {
      markErr("cpf", "CPF invalido");
      ok = false;
    } else {
      clearErr("cpf");
    }

    if (telefone.length < 10) {
      markErr("telefone", "Telefone obrigatorio");
      ok = false;
    } else {
      clearErr("telefone");
    }

    if (!ok) {
      document.querySelector(".field--error")?.scrollIntoView({ behavior: "smooth", block: "center" });
      return false;
    }

    doneStep(2, `${nome} ${sobrenome} - ${val("telefone")}`);
    openStep(3);
    return true;
  }

  function makeFreteOption(name, prazo, price, selected) {
    const priceText = price === 0 ? "Gratis" : money(price);
    const selectedClass = selected ? " selected" : "";
    const priceClass = price === 0 ? " gratis" : "";

    return `
      <button class="delivery-opt${selectedClass}" type="button" data-price="${price}">
        <span class="delivery-opt__left">
          <span class="delivery-opt__radio"></span>
          <span>
            <span class="delivery-opt__name">${name}</span>
            <span class="delivery-opt__prazo">${prazo}</span>
          </span>
        </span>
        <span class="delivery-opt__price${priceClass}">${priceText}</span>
      </button>
    `;
  }

  function showDeliveryOptions(cep) {
    const prefix = Number.parseInt(cep.slice(0, 2), 10);
    let pacDays = 12;
    let sedexDays = 4;
    let pacPrice = 18.9;

    if (prefix >= 1 && prefix <= 19) {
      pacDays = 5;
      sedexDays = 1;
      pacPrice = 0;
    } else if (prefix >= 20 && prefix <= 28) {
      pacDays = 6;
      sedexDays = 2;
      pacPrice = 8.9;
    } else if (prefix >= 30 && prefix <= 38) {
      pacDays = 7;
      sedexDays = 2;
      pacPrice = 10.9;
    } else if (prefix >= 80 && prefix <= 87) {
      pacDays = 7;
      sedexDays = 2;
      pacPrice = 12.9;
    } else if (prefix >= 60 && prefix <= 63) {
      pacDays = 10;
      sedexDays = 3;
      pacPrice = 15.9;
    }

    const sedexPrice = pacPrice === 0 ? 12 : pacPrice + 12;
    const section = byId("delivery-section");
    const options = byId("deliveryOpts");

    if (!section || !options) {
      return;
    }

    fretePrice = pacPrice;
    section.classList.add("show");
    options.innerHTML =
      makeFreteOption("PAC", `Ate ${pacDays} dias uteis`, pacPrice, true) +
      makeFreteOption("SEDEX", `Ate ${sedexDays} dias uteis`, sedexPrice, false) +
      makeFreteOption("Retirada na loja", "Disponivel em 1 dia util", 0, false);

    byId("val-entrega").textContent = pacPrice === 0 ? "Gratis" : money(pacPrice);
    byId("val-entrega").className = `total-row__val${pacPrice === 0 ? " desconto" : ""}`;
    recalcTotal();
  }

  async function buscarCep() {
    const cep = val("cep").replace(/\D/g, "");
    if (cep.length !== 8) {
      markErr("cep", "CEP invalido");
      return;
    }

    const button = byId("btnCep");
    if (button) {
      button.textContent = "...";
      button.disabled = true;
    }

    try {
      const response = await fetch(`https://viacep.com.br/ws/${cep}/json/`);
      const data = await response.json();
      if (data.erro) {
        markErr("cep", "CEP nao encontrado");
        return;
      }

      byId("rua").value = data.logradouro || val("rua");
      byId("bairro").value = data.bairro || val("bairro");
      byId("cidade").value = data.localidade || val("cidade");
      byId("estado").value = data.uf || val("estado");
      clearErr("cep");
      byId("numero")?.focus();
      showDeliveryOptions(cep);
    } catch {
      markErr("cep", "Erro ao buscar CEP");
    } finally {
      if (button) {
        button.textContent = "Buscar";
        button.disabled = false;
      }
    }
  }

  function confirmEntrega() {
    let ok = true;
    const checks = [
      ["cep", val("cep").replace(/\D/g, "").length === 8, "CEP invalido"],
      ["rua", Boolean(val("rua").trim()), "Endereco obrigatorio"],
      ["numero", Boolean(val("numero").trim()), "Numero obrigatorio"],
      ["bairro", Boolean(val("bairro").trim()), "Bairro obrigatorio"],
      ["cidade", Boolean(val("cidade").trim()), "Cidade obrigatoria"],
      ["estado", Boolean(val("estado").trim()), "UF obrigatoria"]
    ];

    checks.forEach(([id, valid, message]) => {
      if (valid) {
        clearErr(id);
      } else {
        markErr(id, message);
        ok = false;
      }
    });

    if (!ok) {
      document.querySelector(".field--error")?.scrollIntoView({ behavior: "smooth", block: "center" });
      return false;
    }

    const freteText = byId("val-entrega")?.textContent || "A calcular";
    doneStep(3, `${val("rua")}, ${val("numero")} - ${val("cidade")}/${val("estado")}`);
    byId("sum-4").textContent = `Entrega: ${freteText}`;
    openStep(4);
    return true;
  }

  function aplicarCupom() {
    const code = val("cupomInput").toUpperCase().trim();
    const message = byId("cupomMsg");
    const discount = cupons[code];

    if (discount) {
      cupomDiscount = discount;
      byId("row-desconto")?.classList.add("show");
      byId("val-desconto").textContent = `- ${money(cupomDiscount)}`;
      if (message) {
        message.style.color = "var(--sage)";
        message.textContent = `Cupom aplicado. Desconto de ${money(cupomDiscount)}.`;
      }
      recalcTotal();
      return;
    }

    if (message) {
      message.style.color = "var(--terracota)";
      message.textContent = "Cupom invalido ou expirado.";
    }
  }

  function confirmStep(step) {
    if (step === 1) {
      return confirmEmail();
    }
    if (step === 2) {
      return confirmDados();
    }
    if (step === 3) {
      return confirmEntrega();
    }
    return true;
  }

  document.querySelectorAll("[data-confirm-step]").forEach((button) => {
    button.addEventListener("click", () => confirmStep(Number(button.dataset.confirmStep)));
  });

  document.querySelectorAll("[data-edit-step]").forEach((button) => {
    button.addEventListener("click", () => editStep(Number(button.dataset.editStep)));
  });

  byId("btnCep")?.addEventListener("click", buscarCep);
  byId("btnCupom")?.addEventListener("click", aplicarCupom);

  byId("deliveryOpts")?.addEventListener("click", (event) => {
    const option = event.target.closest(".delivery-opt");
    if (!option) {
      return;
    }

    document.querySelectorAll(".delivery-opt").forEach((item) => item.classList.remove("selected"));
    option.classList.add("selected");
    fretePrice = Number.parseFloat(option.dataset.price || "0") || 0;
    const priceText = option.querySelector(".delivery-opt__price")?.textContent || "A calcular";
    byId("val-entrega").textContent = priceText;
    byId("val-entrega").className = `total-row__val${fretePrice === 0 ? " desconto" : ""}`;
    recalcTotal();
  });

  byId("email")?.addEventListener("keydown", (event) => {
    if (event.key === "Enter") {
      event.preventDefault();
      confirmEmail();
    }
  });

  byId("cep")?.addEventListener("keydown", (event) => {
    if (event.key === "Enter") {
      event.preventDefault();
      buscarCep();
    }
  });

  document.querySelectorAll(".field input, .field textarea").forEach((input) => {
    input.addEventListener("input", () => {
      input.closest(".field")?.classList.remove("field--error");
    });
  });

  form.addEventListener("submit", (event) => {
    if (currentStep < 4) {
      event.preventDefault();
      confirmStep(currentStep);
      return;
    }

    const valid = confirmEmail() && confirmDados() && confirmEntrega();
    if (!valid) {
      event.preventDefault();
      return;
    }

    doneStep(4, "Pedido enviado para confirmacao");
    const button = byId("btnFinalizar");
    if (button) {
      button.textContent = "Processando...";
      button.disabled = true;
    }
  });

  setProgress(1);
  recalcTotal();
}

initCheckoutDynamics();

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
