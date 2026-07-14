const navbar = document.getElementById("navbar");
const topRibbon = document.getElementById("topRibbon");
const accountMenu = document.getElementById("accountMenu");
const accountMenuToggle = document.getElementById("accountMenuToggle");

function syncNavState() {
  const scrolled = window.scrollY > 0;
  navbar?.classList.toggle("scrolled", scrolled);
  navbar?.classList.toggle("ribbon-hidden", scrolled);
  topRibbon?.classList.toggle("hidden", scrolled);
}

window.addEventListener("scroll", syncNavState, { passive: true });
syncNavState();

function closeAccountMenu() {
  accountMenu?.classList.remove("open");
  accountMenuToggle?.setAttribute("aria-expanded", "false");
}

accountMenuToggle?.addEventListener("click", (event) => {
  event.stopPropagation();
  const isOpen = accountMenu?.classList.toggle("open") ?? false;
  accountMenuToggle.setAttribute("aria-expanded", String(isOpen));
});

document.addEventListener("click", (event) => {
  if (!accountMenu?.contains(event.target)) {
    closeAccountMenu();
  }
});

const hamburger = document.getElementById("hamburger");
const mobileMenu = document.getElementById("mobileMenu");
const mobileClose = document.getElementById("mobileClose");
const mobileOverlay = document.getElementById("mobileOverlay");

function openMenu() {
  closeAccountMenu();
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
const bagDrawer = document.getElementById("bagDrawer");
const bagOverlay = document.getElementById("bagOverlay");
const bagBtn = document.getElementById("bagBtn");
const bagClose = document.getElementById("bagClose");
const bagBody = document.getElementById("bagBody");
const bagEmpty = document.getElementById("bagEmpty");
const bagBadge = document.getElementById("bagBadge");
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
const cartBagPanel = document.getElementById("cartBagPanel");
const cartBagCount = document.getElementById("cartBagCount");
const cartBagDeadline = document.getElementById("cartBagDeadline");
const finalizeBagBtn = document.getElementById("finalizeBagBtn");
const token = document.querySelector("meta[name='request-verification-token']")?.content;
let currentCartSubtotal = 0;
let currentActiveBagId = "";

function syncDrawerBodyScroll() {
  const hasOpenPanel = Boolean(
    mobileMenu?.classList.contains("open") ||
    cartDrawer?.classList.contains("open") ||
    bagDrawer?.classList.contains("open")
  );
  document.body.style.overflow = hasOpenPanel ? "hidden" : "";
}

function setDrawerOpen(drawer, overlay, isOpen) {
  drawer?.classList.toggle("open", isOpen);
  drawer?.setAttribute("aria-hidden", String(!isOpen));
  overlay?.classList.toggle("open", isOpen);
  syncDrawerBodyScroll();
}

function openCart() {
  closeBag();
  closeAccountMenu();
  setDrawerOpen(cartDrawer, cartOverlay, true);
}

function closeCart() {
  setDrawerOpen(cartDrawer, cartOverlay, false);
}

function openBag() {
  closeCart();
  closeAccountMenu();
  renderPaidOrders();
  setDrawerOpen(bagDrawer, bagOverlay, true);
}

function closeBag() {
  setDrawerOpen(bagDrawer, bagOverlay, false);
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

function readSessionJson(key) {
  try {
    const raw = sessionStorage.getItem(key);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

function getStoredOrders() {
  const orders = [];

  try {
    for (let index = 0; index < sessionStorage.length; index += 1) {
      const key = sessionStorage.key(index);
      if (!key?.startsWith("aliebrecho:order-confirm:")) {
        continue;
      }

      const order = readSessionJson(key);
      const isBagOrder = order?.deliveryType === "bag" || order?.delivery?.mode === "Sacolinha";
      if (order?.orderId && isBagOrder) {
        orders.push(order);
      }
    }
  } catch {
    return [];
  }

  return orders.sort((a, b) => new Date(b.generatedAt || 0) - new Date(a.generatedAt || 0));
}

function updateBagBadge() {
  if (!bagBadge) {
    return;
  }

  bagBadge.textContent = getStoredOrders().length;
}

function markStoredOrderAsPaid(orderId) {
  if (!orderId) {
    return;
  }

  const key = `aliebrecho:order-confirm:${orderId}`;
  const order = readSessionJson(key);
  if (!order) {
    return;
  }

  try {
    order.status = "paid";
    order.statusText = "Pago";
    order.paidAt = new Date().toISOString();
    sessionStorage.setItem(key, JSON.stringify(order));
    updateBagBadge();
  } catch {
    // Mantem a sacolinha funcional mesmo se o snapshot local estiver invalido.
  }
}

function removeStoredBagOrder(bagId) {
  if (!bagId) {
    return false;
  }

  const keysToRemove = [];
  try {
    for (let index = 0; index < sessionStorage.length; index += 1) {
      const key = sessionStorage.key(index);
      if (!key?.startsWith("aliebrecho:order-confirm:")) {
        continue;
      }

      const order = readSessionJson(key);
      const orderId = order?.orderId || key.replace("aliebrecho:order-confirm:", "");
      const isBagOrder = order?.deliveryType === "bag" || order?.delivery?.mode === "Sacolinha";
      if (isBagOrder && orderId === bagId) {
        keysToRemove.push(key, `aliebrecho:pix:${orderId}`);
      }
    }

    keysToRemove.forEach((key) => sessionStorage.removeItem(key));
  } catch {
    return false;
  }

  return keysToRemove.length > 0;
}

function renderPaidOrders() {
  updateBagBadge();

  if (!bagBody || !bagEmpty) {
    return;
  }

  const orders = getStoredOrders();
  if (!orders.length) {
    bagBody.innerHTML = "";
    bagBody.appendChild(bagEmpty);
    return;
  }

  bagBody.innerHTML = "";
  orders.forEach((order) => {
    const orderBlock = document.createElement("div");
    orderBlock.className = "bag-order";
    const orderId = escapeHtml(order.orderId);
    const status = escapeHtml(order.statusText || (order.status === "paid" ? "Pago" : "Pagamento iniciado"));
    const items = Array.isArray(order.items) ? order.items : [];
    const itemCount = items.reduce((total, item) => total + (Number(item.quantity) || 1), 0);

    const meta = document.createElement("div");
    meta.className = "bag-order__meta";
    meta.innerHTML = `
      <span>Pedido ${orderId}</span>
      <span class="bag-order__status">${status}</span>
      <span>${itemCount} ${itemCount === 1 ? "peca" : "pecas"}</span>
    `;
    orderBlock.appendChild(meta);

    items.forEach((item) => {
      const row = document.createElement("div");
      row.className = "cart-item";
      const name = escapeHtml(item.name || "Produto");
      const image = escapeHtml(item.image || "");
      const variant = escapeHtml(item.variant || "Tamanho unico");
      const price = escapeHtml(item.price || "");
      const quantity = escapeHtml(item.quantity || "1");
      const media = item.image
        ? `<img src="${image}" alt="${name}">`
        : `<span>${name}</span>`;

      row.innerHTML = `
        <div class="cart-item__img">${media}</div>
        <div class="cart-item__info">
          <div class="cart-item__name">${name}</div>
          <div class="cart-item__size">${variant}</div>
          <div class="cart-item__qty-row">
            <span class="qty-val">${quantity} unidade</span>
          </div>
        </div>
        <div class="cart-item__price">${price}</div>
      `;

      orderBlock.appendChild(row);
    });

    bagBody.appendChild(orderBlock);
  });
}

function renderCart(cart) {
  currentCartSubtotal = Number(cart?.subtotal ?? 0);
  currentActiveBagId = cart?.activeBag?.id || "";

  if (cartTotal) {
    cartTotal.textContent = cart?.subtotalText ?? "R$ 0,00";
  }

  if (cartBadge) {
    cartBadge.textContent = cart?.itemCount ?? 0;
  }

  if (!cartBody || !cartEmpty) {
    renderActiveBag(cart?.activeBag);
    return;
  }

  if (!cart?.items?.length) {
    cartBody.innerHTML = "";
    cartBody.appendChild(cartEmpty);
    renderActiveBag(cart?.activeBag);
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

  renderActiveBag(cart?.activeBag);
}

function renderActiveBag(activeBag) {
  if (!cartBagPanel) {
    return;
  }

  if (!activeBag?.id) {
    cartBagPanel.hidden = true;
    return;
  }

  cartBagPanel.hidden = false;
  if (cartBagCount) {
    const itemCount = Number(activeBag.itemCount || 0);
    cartBagCount.textContent = `${itemCount} ${itemCount === 1 ? "peca" : "pecas"}`;
  }
  if (cartBagDeadline) {
    cartBagDeadline.textContent = `Prazo: ${activeBag.expirationDateText || "a confirmar"}`;
  }
}

async function handleBagChanged(event) {
  const bagId = event?.bagId || event?.id || "";
  const changeType = String(event?.changeType || "").toLowerCase();
  const isDeleted = event?.isDeleted === true || changeType === "deleted";

  if (!isDeleted) {
    return;
  }

  const removedLocalOrder = removeStoredBagOrder(bagId);
  if (!bagId || currentActiveBagId === bagId) {
    currentActiveBagId = "";
    renderActiveBag(null);
  }

  renderPaidOrders();
  await fetchCart().catch(() => {});

  if (removedLocalOrder || bagId) {
    showToast("Sacolinha atualizada.");
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

async function finalizeBagNow(bagId) {
  if (!bagId) {
    showToast("Nenhuma sacolinha em andamento.");
    return false;
  }

  const confirmed = window.confirm(
    "Finalizar a sacolinha agora? O frete sera recalculado com as pecas acumuladas e o envio sera solicitado imediatamente."
  );
  if (!confirmed) {
    return false;
  }

  const formData = new FormData();
  formData.append("bagId", bagId);

  const response = await fetch("/Cart?handler=FinalizeBag", {
    method: "POST",
    headers: cartHeaders(),
    body: formData
  });

  const data = await response.json().catch(() => ({}));
  if (!response.ok) {
    throw new Error(data.message || "Nao foi possivel finalizar a sacolinha.");
  }

  showToast("Sacolinha finalizada. O frete foi recalculado para envio imediato.");
  await fetchCart().catch(() => {});
  return true;
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
bagBtn?.addEventListener("click", async () => {
  try {
    await fetchCart();
  } catch {
    currentActiveBagId = "";
  }

  openBag();
});
bagClose?.addEventListener("click", closeBag);
bagOverlay?.addEventListener("click", closeBag);

finalizeBagBtn?.addEventListener("click", async () => {
  try {
    await finalizeBagNow(currentActiveBagId);
  } catch (error) {
    showToast(error.message || "Nao foi possivel finalizar a sacolinha.");
  }
});

document.querySelectorAll("[data-finalize-bag-form]").forEach((form) => {
  form.addEventListener("submit", (event) => {
    if (!window.confirm("Finalizar a sacolinha agora? O frete sera recalculado para envio imediato.")) {
      event.preventDefault();
    }
  });
});

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
  if (event.key === "Escape") {
    closeAccountMenu();
  }

  if (event.key === "Escape" && cartDrawer?.classList.contains("open")) {
    closeCart();
  }

  if (event.key === "Escape" && bagDrawer?.classList.contains("open")) {
    closeBag();
  }
});

fetchCart().catch(() => {
  renderCart({ itemCount: 0, subtotalText: "R$ 0,00", items: [] });
});
updateBagBadge();

function initCheckoutDynamics() {
  const form = document.getElementById("checkoutForm");
  if (!form) {
    return;
  }

  let currentStep = 1;
  let fretePrice = 0;
  let cupomDiscount = 0;
  let selectedDeliveryMode = "normal";
  const totalSteps = 5;
  const basePrice = Number.parseFloat(form.dataset.subtotal || "0") || 0;
  const cupons = { ALIE10: 10, BRECHO15: 15, BEMVINDA: 20 };

  const byId = (id) => document.getElementById(id);
  const val = (id) => byId(id)?.value || "";
  const money = (value) => value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });

  function applyMask(id, fn) {
    const input = byId(id);
    if (input) {
      input.value = fn(input.value);
    }

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
    for (let index = 1; index <= totalSteps; index += 1) {
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
    for (let index = 1; index <= totalSteps; index += 1) {
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
    for (let index = step; index <= totalSteps; index += 1) {
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
    if (byId("val-total")) {
      byId("val-total").textContent = money(total);
    }
  }

  function isBagDelivery() {
    return selectedDeliveryMode === "bag";
  }

  function getDeliveryModeSummary() {
    if (isBagDelivery()) {
      return "Sacolinha: frete recalculado no envio conjunto";
    }

    const selectedDelivery = document.querySelector(".delivery-opt.selected");
    const deliveryName = selectedDelivery?.querySelector(".delivery-opt__name")?.textContent?.trim() || "PAC";
    const freteText = byId("val-entrega")?.textContent || "A calcular";
    return `${deliveryName}: ${freteText}`;
  }

  function selectDeliveryMode(mode) {
    selectedDeliveryMode = mode === "bag" ? "bag" : "normal";
    const input = byId("deliveryMode");
    if (input) {
      input.value = selectedDeliveryMode;
    }

    document.querySelectorAll(".shipping-choice-card").forEach((card) => {
      card.classList.toggle("selected", card.dataset.deliveryMode === selectedDeliveryMode);
    });

    if (isBagDelivery()) {
      fretePrice = 0;
      byId("delivery-section")?.classList.remove("show");
      if (byId("val-entrega")) {
        byId("val-entrega").textContent = "Recalculado ao finalizar a sacolinha";
        byId("val-entrega").className = "total-row__val calcular";
      }
      recalcTotal();
      return;
    }

    const cep = val("cep").replace(/\D/g, "");
    if (cep.length === 8) {
      showDeliveryOptions(cep);
    } else if (byId("val-entrega")) {
      byId("val-entrega").textContent = "A calcular";
      byId("val-entrega").className = "total-row__val calcular";
      recalcTotal();
    }
  }

  function maskCpf(value) {
    const digits = String(value || "").replace(/\D/g, "");
    if (digits.length !== 11) {
      return "***.***.***-**";
    }

    return `${digits.slice(0, 3)}.***.***-${digits.slice(9)}`;
  }

  function maskName(firstName, lastName) {
    const parts = [firstName, lastName].map((part) => String(part || "").trim()).filter(Boolean);
    return parts.map((part, index) => {
      if (part.length <= 2) {
        return `${part[0] || "*"}*`;
      }
      return index === 0 ? `${part[0]}${"*".repeat(Math.max(2, part.length - 1))}` : `${part[0]}***`;
    }).join(" ");
  }

  function maskText(value) {
    const text = String(value || "").trim();
    if (!text) {
      return "--";
    }

    if (text.length <= 3) {
      return `${text[0]}**`;
    }

    return `${text.slice(0, 3)}***`;
  }

  function collectCheckoutSnapshot(orderId) {
    const selectedDelivery = document.querySelector(".delivery-opt.selected");
    const deliveryName = isBagDelivery()
      ? "Sacolinha"
      : selectedDelivery?.querySelector(".delivery-opt__name")?.textContent?.trim() || "PAC";
    const deliveryDeadline = isBagDelivery()
      ? "Envio retido ate a finalizacao da sacolinha"
      : selectedDelivery?.querySelector(".delivery-opt__prazo")?.textContent?.trim() || "Ate 5 dias uteis";
    const deliveryPrice = byId("val-entrega")?.textContent?.trim() || "A calcular";
    const items = Array.from(document.querySelectorAll(".checkout-cart-item")).map((item) => ({
      name: item.querySelector(".checkout-cart-item__name")?.textContent?.trim() || "Produto",
      variant: item.querySelector(".checkout-cart-item__variant")?.textContent?.trim() || "Tamanho unico",
      price: item.querySelector(".checkout-cart-item__price")?.textContent?.trim() || "",
      image: item.querySelector(".checkout-cart-item__img img")?.getAttribute("src") || "",
      quantity: item.querySelector(".checkout-cart-item__badge")?.textContent?.trim() || "1"
    }));

    const snapshot = {
      orderId,
      deliveryType: isBagDelivery() ? "bag" : "normal",
      generatedAt: new Date().toISOString(),
      expiresAt: new Date(Date.now() + 30 * 60 * 1000).toISOString(),
      status: "payment_started",
      statusText: "Pagamento iniciado",
      customer: {
        name: maskName(val("nome"), val("sobrenome")),
        cpf: maskCpf(val("cpf")),
        email: val("email").trim()
      },
      delivery: {
        address: `${maskText(val("rua"))}, ${maskText(val("numero"))}${val("complemento") ? `, ${maskText(val("complemento"))}` : ""} - ${maskText(val("bairro"))}, ${maskText(val("cidade"))}/${String(val("estado") || "").toUpperCase() || "**"} - CEP ${maskText(val("cep"))}`,
        carrier: "Correios",
        mode: deliveryName,
        deadline: deliveryDeadline,
        price: deliveryPrice
      },
      totals: {
        subtotal: byId("val-subtotal")?.textContent?.trim() || money(basePrice),
        discount: byId("val-desconto")?.textContent?.trim() || "- R$ 0,00",
        shipping: deliveryPrice,
        total: byId("val-total")?.textContent?.trim() || money(basePrice + fretePrice - cupomDiscount)
      },
      items
    };

    sessionStorage.setItem(`aliebrecho:order-confirm:${orderId}`, JSON.stringify(snapshot));
    updateBagBadge();
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
    if (isBagDelivery()) {
      selectDeliveryMode("bag");
      return;
    }

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

    if (!isBagDelivery() && !document.querySelector(".delivery-opt.selected")) {
      showDeliveryOptions(val("cep").replace(/\D/g, ""));
    }

    doneStep(3, `${val("rua")}, ${val("numero")} - ${val("cidade")}/${val("estado")}`);
    openStep(4);
    return true;
  }

  function confirmRecebimento() {
    if (!isBagDelivery() && !document.querySelector(".delivery-opt.selected")) {
      const cep = val("cep").replace(/\D/g, "");
      if (cep.length === 8) {
        showDeliveryOptions(cep);
      }
    }

    doneStep(4, getDeliveryModeSummary());
    openStep(5);
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

  function getPaymentMethodLabel(method) {
    return "Pix";
  }

  function selectPaymentMethod(method) {
    const normalized = "pix";
    const input = byId("paymentMethod");
    if (input) {
      input.value = normalized;
    }

    document.querySelectorAll(".payment-method-card").forEach((card) => {
      card.classList.toggle("selected", card.dataset.paymentMethod === normalized);
    });

    const summary = byId("sum-5");
    if (summary) {
      summary.textContent = getPaymentMethodLabel(normalized);
    }
  }

  function hasValidPaymentMethod() {
    const method = val("paymentMethod");
    return method === "pix";
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
    if (step === 4) {
      return confirmRecebimento();
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

  byId("shippingChoice")?.addEventListener("click", (event) => {
    const option = event.target.closest(".shipping-choice-card");
    if (!option) {
      return;
    }

    selectDeliveryMode(option.dataset.deliveryMode);
  });

  byId("paymentMethods")?.addEventListener("click", (event) => {
    const option = event.target.closest(".payment-method-card");
    if (!option) {
      return;
    }

    selectPaymentMethod(option.dataset.paymentMethod);
  });

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

  form.addEventListener("submit", async (event) => {
    event.preventDefault();

    if (currentStep < totalSteps) {
      confirmStep(currentStep);
      return;
    }

    const valid = confirmEmail() && confirmDados() && confirmEntrega() && confirmRecebimento();
    if (!valid) {
      return;
    }

    if (!hasValidPaymentMethod()) {
      showToast("Escolha Pix.");
      return;
    }

    doneStep(5, "Redirecionando para pagamento");
    const button = byId("btnFinalizar");
    if (button) {
      button.textContent = "Processando...";
      button.disabled = true;
    }

    try {
      const response = await fetch(form.action || window.location.href, {
        method: "POST",
        body: new FormData(form),
        headers: {
          "X-Requested-With": "XMLHttpRequest"
        }
      });

      const data = await response.json().catch(() => ({}));
      const hasPixData = Boolean(data.pixQrCode || data.pixCode);
      if (!response.ok || (!data.paymentUrl && !hasPixData)) {
        throw new Error(data.message || "Nao foi possivel iniciar o pagamento.");
      }

      if (data.orderId) {
        sessionStorage.setItem("aliebrecho:lastOrderId", data.orderId);
        collectCheckoutSnapshot(data.orderId);
      }

      if (data.orderId && (data.pixQrCode || data.pixCode)) {
        sessionStorage.setItem(`aliebrecho:pix:${data.orderId}`, JSON.stringify({
          pixQrCode: data.pixQrCode || "",
          pixCode: data.pixCode || "",
          paymentId: data.paymentId || ""
        }));
        window.location.href = `/Payment/Pix/${encodeURIComponent(data.orderId)}`;
        return;
      }

      window.location.href = data.paymentUrl;
    } catch (error) {
      showToast(error.message || "Nao foi possivel iniciar o pagamento.");
      if (button) {
        button.textContent = "Pagar";
        button.disabled = false;
      }
    }
  });

  setProgress(1);
  selectDeliveryMode(val("deliveryMode") || "normal");
  selectPaymentMethod(val("paymentMethod") || "pix");
  recalcTotal();
}

initCheckoutDynamics();

function initPixPaymentPage() {
  const shell = document.querySelector(".pix-payment-shell");
  if (!shell) {
    return;
  }

  const orderId = shell.dataset.orderId;
  const help = document.getElementById("pixPaymentHelp");
  const copyCode = document.getElementById("pixCopyCode");
  const copyPreview = document.getElementById("pixCopyPreview");
  const qrImage = document.getElementById("pixQrImage");
  const qrText = document.getElementById("pixQrText");
  const raw = orderId ? sessionStorage.getItem(`aliebrecho:pix:${orderId}`) : null;
  const snapshot = orderId ? readSessionJson(`aliebrecho:order-confirm:${orderId}`) : null;
  const isBagPayment = snapshot?.deliveryType === "bag" || snapshot?.delivery?.mode === "Sacolinha";
  let pollTimer = null;

  const moneyFallback = "--";
  const formatDateTime = (value) => {
    const date = value ? new Date(value) : new Date(Date.now() + 30 * 60 * 1000);
    return date.toLocaleString("pt-BR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit"
    });
  };

  const setText = (id, value) => {
    const element = document.getElementById(id);
    if (element) {
      element.textContent = value || "--";
    }
  };

  const truncateMiddle = (value, start = 28, end = 18) => {
    const text = String(value || "");
    if (text.length <= start + end + 3) {
      return text || "--";
    }

    return `${text.slice(0, start)}...${text.slice(-end)}`;
  };

  const escapeHtml = (value) => String(value || "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");

  const renderSnapshot = () => {
    const totals = snapshot?.totals || {};
    const delivery = snapshot?.delivery || {};
    const customer = snapshot?.customer || {};

    if (isBagPayment) {
      setText("pixConfirmationEyebrow", "Confirmacao da sacolinha");
      const title = document.getElementById("pixConfirmationTitle");
      if (title) {
        title.innerHTML = "Recebemos sua <em>sacolinha!</em>";
      }
      const orderNumberLabel = document.getElementById("pixOrderNumberLabel");
      if (orderNumberLabel) {
        orderNumberLabel.innerHTML = `Numero da sacolinha: <strong>${escapeHtml(orderId)}</strong>`;
      }
      const summaryTitle = document.getElementById("pixSummaryTitle");
      if (summaryTitle) {
        summaryTitle.innerHTML = "Resumo da <em>sacolinha</em>";
      }
      setText("pixFallbackItemName", "Produto da sacolinha");
      setText("pixSubtotalLabel", "Subtotal da sacolinha");
    }

    setText("pixTotalValue", totals.total || moneyFallback);
    setText("pixExpiresAt", formatDateTime(snapshot?.expiresAt));
    setText("pixCpf", customer.cpf || "***.***.***-**");
    setText("pixEmail", customer.email || "--");
    setText("pixCustomerName", customer.name || "--");
    setText("pixAddress", delivery.address || "--");
    setText("pixDeliveryEstimate", delivery.deadline || "--");
    setText("pixSubtotal", totals.subtotal || moneyFallback);
    setText("pixDiscount", totals.discount || "- R$ 0,00");
    setText("pixShipping", totals.shipping || moneyFallback);
    setText("pixSummaryTotal", totals.total || moneyFallback);
    setText("pixShippingLabel", `Frete${delivery.carrier || delivery.mode ? ` (${[delivery.carrier, delivery.mode].filter(Boolean).join(" - ")})` : ""}`);

    const list = document.getElementById("pixSummaryItems");
    if (list && snapshot?.items?.length) {
      list.innerHTML = snapshot.items.map((item) => `
        <div class="checkout-cart-item">
          <div class="checkout-cart-item__img">
            ${item.image ? `<img src="${escapeHtml(item.image)}" alt="${escapeHtml(item.name)}" />` : `<span>${escapeHtml(item.name)}</span>`}
            <div class="checkout-cart-item__badge">${escapeHtml(item.quantity || "1")}</div>
          </div>
          <div class="checkout-cart-item__info">
            <div class="checkout-cart-item__name">${escapeHtml(item.name)}</div>
            <div class="checkout-cart-item__variant">${escapeHtml(item.variant)}</div>
            <div class="checkout-cart-item__pricing">
              <span class="checkout-cart-item__price">${escapeHtml(item.price || moneyFallback)}</span>
            </div>
          </div>
        </div>
      `).join("");
    }
  };

  renderSnapshot();

  if (!raw) {
    if (help) {
      help.textContent = "Nao encontramos os dados Pix desta sessao. Volte ao checkout para gerar o pagamento novamente.";
    }
    return;
  }

  const data = JSON.parse(raw);
  const pixCode = data.pixCode || data.pixQrCode || "";
  if (copyCode) {
    copyCode.value = pixCode;
  }
  if (copyPreview) {
    copyPreview.textContent = truncateMiddle(pixCode);
    copyPreview.title = pixCode;
  }

  if (data.pixQrCode && /^(data:image\/|https?:\/\/)/i.test(data.pixQrCode) && qrImage) {
    qrImage.src = data.pixQrCode;
    qrImage.hidden = false;
  } else if (data.pixQrCode && qrText) {
    qrText.textContent = data.pixQrCode;
    qrText.hidden = false;
  }

  const stopPolling = () => {
    if (pollTimer) {
      window.clearInterval(pollTimer);
      pollTimer = null;
    }
  };

  const isApproved = (status) => {
    return String(status || "").toLowerCase() === "approved" ||
      String(status || "").toLowerCase() === "paid";
  };

  const checkPaymentStatus = async () => {
    if (!data.paymentId) {
      if (help) {
        help.textContent = "Pagamento gerado. Quando confirmar o Pix, acompanhe seu pedido pela sua conta.";
      }
      stopPolling();
      return;
    }

    try {
      const response = await fetch(
        `/Payment/Pix/${encodeURIComponent(orderId)}?handler=Status&paymentId=${encodeURIComponent(data.paymentId)}`,
        { headers: { Accept: "application/json" } }
      );
      const statusData = await response.json().catch(() => ({}));
      if (!response.ok) {
        throw new Error(statusData.message || "Nao foi possivel consultar o pagamento.");
      }

      if (isApproved(statusData.status) || isApproved(statusData.orderStatus)) {
        stopPolling();
        markStoredOrderAsPaid(orderId);
        sessionStorage.removeItem(`aliebrecho:pix:${orderId}`);
        if (help) {
          help.textContent = "Pagamento confirmado. Redirecionando...";
        }
        window.location.href = `/obrigado?pedido=${encodeURIComponent(orderId)}`;
        return;
      }

      if (help) {
        help.textContent = "Aguardando confirmacao do pagamento Pix...";
      }
    } catch (error) {
      if (help) {
        help.textContent = error.message || "Nao foi possivel consultar o pagamento agora.";
      }
    }
  };

  checkPaymentStatus();
  pollTimer = window.setInterval(checkPaymentStatus, 5000);

  document.getElementById("copyPixCode")?.addEventListener("click", async () => {
    try {
      await navigator.clipboard.writeText(copyCode?.value || "");
      showToast("Codigo Pix copiado.");
    } catch {
      showToast("Nao foi possivel copiar o codigo Pix.");
    }
  });

  document.getElementById("refreshPixPage")?.addEventListener("click", () => {
    checkPaymentStatus();
    window.location.reload();
  });

  window.addEventListener("beforeunload", stopPolling, { once: true });
}

initPixPaymentPage();

function initThanksPage() {
  const shell = document.querySelector(".thanks-shell[data-thanks-order-id]");
  if (!shell) {
    return;
  }

  const orderId = shell.dataset.thanksOrderId;
  if (!orderId) {
    return;
  }

  const snapshot = readSessionJson(`aliebrecho:order-confirm:${orderId}`);
  if (!snapshot) {
    return;
  }

  const isBagOrder = snapshot?.deliveryType === "bag" || snapshot?.delivery?.mode === "Sacolinha";
  if (!isBagOrder) {
    return;
  }

  const panel = shell.querySelector(".thanks-panel");
  if (!panel) {
    return;
  }

  panel.innerHTML = `
    <p class="eyebrow">Pagamento confirmado</p>
    <h1>Obrigado pela sua compra!</h1>
    <p class="thanks-text">Sua sacolinha foi confirmada e as pecas pagas ficaram indisponiveis para novas compras.</p>

    <div class="thanks-summary" aria-label="Resumo da sacolinha">
      <div>
        <span>Sacolinha</span>
        <strong>${escapeHtml(orderId)}</strong>
      </div>
      <div>
        <span>Valor pago</span>
        <strong>${escapeHtml(snapshot?.totals?.total || "--")}</strong>
      </div>
      <div>
        <span>Forma de pagamento</span>
        <strong>Pix</strong>
      </div>
      <div>
        <span>Status</span>
        <strong>${escapeHtml(snapshot?.statusText || "Pago")}</strong>
      </div>
    </div>

    <div class="thanks-actions">
      <a class="btn-confirm" href="/#pecas">Voltar para a loja</a>
      <a class="btn-confirm btn-confirm-secondary" href="/Orders">Acompanhar pedido</a>
    </div>
  `;
}

initThanksPage();

async function initInstagramFeed() {
  const grid = document.getElementById("instagramGrid");
  if (!grid) {
    return;
  }

  const endpoint = grid.dataset.instagramEndpoint || "/api/instagram/latest";

  try {
    const response = await fetch(endpoint, {
      headers: { Accept: "application/json" }
    });

    if (!response.ok) {
      throw new Error("Nao foi possivel carregar o Instagram.");
    }

    const posts = await response.json();
    if (!Array.isArray(posts) || posts.length === 0) {
      return;
    }

    grid.innerHTML = posts.slice(0, 6).map((post, index) => {
      const imageUrl = escapeHtml(post.imageUrl);
      const permalink = escapeHtml(post.permalink || "https://www.instagram.com/alie.brecho/");
      const caption = escapeHtml(post.caption || `Publicacao Alie Brecho ${index + 1}`);
      const likes = Number(post.likeCount ?? 0).toLocaleString("pt-BR");
      const comments = Number(post.commentsCount ?? 0).toLocaleString("pt-BR");

      return `
        <a class="insta-card" href="${permalink}" target="_blank" rel="noreferrer">
          <div class="insta-card__img">
            <img src="${imageUrl}" alt="${caption}" loading="lazy">
            <div class="insta-card__overlay">
              <div class="insta-card__stats">
                <span>${likes} curtidas</span>
                <span>${comments} comentarios</span>
              </div>
            </div>
          </div>
        </a>
      `;
    }).join("");
  } catch {
    showToast("Instagram indisponivel. Exibindo imagens locais.");
  }
}

initInstagramFeed();

async function initDropCountdown() {
  const banner = document.getElementById("drop");
  if (!banner) {
    return;
  }

  const endpoint = banner.dataset.dropEndpoint || "/api/drop-config/active";
  const title = banner.querySelector(".drop-banner__title") || document.getElementById("dropTitle");
  const subtitle = banner.querySelector(".drop-banner__desc") || document.getElementById("dropSubtitle");
  const actionButton = document.getElementById("dropActionButton");
  const daysEl = document.getElementById("cd-days");
  const hoursEl = document.getElementById("cd-hours");
  const minsEl = document.getElementById("cd-mins");
  const secsEl = document.getElementById("cd-secs");

  const setTime = (days, hours, mins, secs) => {
    if (daysEl) daysEl.textContent = String(days).padStart(2, "0");
    if (hoursEl) hoursEl.textContent = String(hours).padStart(2, "0");
    if (minsEl) minsEl.textContent = String(mins).padStart(2, "0");
    if (secsEl) secsEl.textContent = String(secs).padStart(2, "0");
  };

  try {
    const response = await fetch(endpoint, {
      headers: { Accept: "application/json" }
    });

    if (!response.ok) {
      banner.hidden = true;
      return;
    }

    const drop = await response.json();
    const releaseDateValue = drop.dataLiberacaoUtc || drop.dataLiberacao;
    const releaseDate = new Date(releaseDateValue);

    if (Number.isNaN(releaseDate.getTime())) {
      banner.hidden = true;
      return;
    }

    if (title && drop.titulo) {
      title.innerHTML = `${escapeHtml(drop.titulo)}<br>CHEGANDO<br><em>EM BREVE</em>`;
    }

    if (subtitle && drop.subtitulo) {
      subtitle.textContent = drop.subtitulo;
    }

    let intervalId;
    let isReleased = false;
    const tick = () => {
      const remaining = releaseDate.getTime() - Date.now();

      if (remaining <= 0) {
        isReleased = true;
        window.clearInterval(intervalId);
        setTime(0, 0, 0, 0);

        if (title && drop.titulo) {
          title.innerHTML = `${escapeHtml(drop.titulo)}<br><em>LIBERADO</em>`;
        }

        if (actionButton) {
          actionButton.textContent = "DROP LIBERADO";
        }
        return;
      }

      const totalSeconds = Math.floor(remaining / 1000);
      const days = Math.floor(totalSeconds / 86400);
      const hours = Math.floor((totalSeconds % 86400) / 3600);
      const mins = Math.floor((totalSeconds % 3600) / 60);
      const secs = totalSeconds % 60;
      setTime(days, hours, mins, secs);
    };

    actionButton?.addEventListener("click", () => {
      if (isReleased) {
        document.getElementById("pecas")?.scrollIntoView({ behavior: "smooth" });
        return;
      }

      showToast("Voce vai ser avisada quando o drop liberar.");
    });

    tick();
    intervalId = window.setInterval(tick, 1000);
  } catch {
    banner.hidden = true;
  }
}

initDropCountdown();

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

function initCatalogNotifications() {
  const catalogSection = document.getElementById("pecas");
  if (!catalogSection) {
    return;
  }

  const getRenderedSignature = () => Array.from(document.querySelectorAll("[data-product-card-id]"))
    .map((item) => item.dataset.productCardSignature || item.dataset.productCardId)
    .filter(Boolean)
    .sort()
    .join("|");

  let renderedSignature = getRenderedSignature();
  let refreshTimeout = null;
  let isReloading = false;

  const scheduleRefresh = () => {
    if (isReloading) {
      return;
    }

    isReloading = true;
    window.clearTimeout(refreshTimeout);
    refreshTimeout = window.setTimeout(() => {
      window.location.reload();
    }, 350);
  };

  const connectSignalR = () => {
    const hubUrl = window.alieBrechoCatalogHubUrl;
    if (!hubUrl || !window.signalR) {
      return;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { withCredentials: false })
      .withAutomaticReconnect()
      .build();

    connection.on("ProductChanged", scheduleRefresh);
    connection.start().catch(() => {
      // A checagem periodica abaixo mantem a vitrine atualizada se o realtime falhar.
    });
  };

  const checkCatalogSnapshot = async () => {
    try {
      const response = await fetch(`/api/catalog/snapshot${window.location.search}`, {
        headers: { Accept: "application/json" },
        cache: "no-store"
      });

      if (!response.ok) {
        return;
      }

      const snapshot = await response.json();
      const nextSignature = typeof snapshot.signature === "string"
        ? snapshot.signature
        : Array.isArray(snapshot.productIds)
          ? snapshot.productIds.filter(Boolean).sort().join("|")
          : "";

      if (nextSignature !== renderedSignature) {
        scheduleRefresh();
        return;
      }

      if (nextSignature) {
        renderedSignature = nextSignature;
      }
    } catch {
      // Se a API oscilar, a pagina continua funcionando com os dados renderizados.
    }
  };

  connectSignalR();
  window.setInterval(checkCatalogSnapshot, 5000);
  checkCatalogSnapshot();
}

function initBagNotifications() {
  const hubUrl = window.alieBrechoOrdersHubUrl;
  if (!hubUrl || !window.signalR) {
    return;
  }

  const connection = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl, { withCredentials: false })
    .withAutomaticReconnect()
    .build();

  connection.on("BagChanged", handleBagChanged);
  connection.start().catch(() => {
    // O carrinho ainda consulta a API quando aberto, mesmo sem realtime.
  });
}

initCatalogNotifications();
initBagNotifications();
