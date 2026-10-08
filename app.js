"use strict";

const APP_STORAGE_KEY = "aurora-pos-data-v2";
const LEGACY_SALES_KEY = "aurora-pos-sales-v1";
const money = new Intl.NumberFormat("es-MX", { style: "currency", currency: "USD" });
const products = [
  { id: "vainilla", name: "Vainilla", category: "Clásicos", price: 12.9, art: "vanilla", icon: "fa-ice-cream" },
  { id: "fresa", name: "Fresa intensa", category: "Frutales", price: 13.5, art: "strawberry", icon: "fa-apple-whole" },
  { id: "mint-oreo", name: "Mint & Oreo", category: "Especiales", price: 14.2, art: "mint", icon: "fa-leaf" },
  { id: "chocolate", name: "Chocolate", category: "Clásicos", price: 13.9, art: "chocolate", icon: "fa-cookie-bite" },
  { id: "mango", name: "Mango tropical", category: "Frutales", price: 13.5, art: "mango", icon: "fa-apple-whole" },
  { id: "limon", name: "Limón", category: "Frutales", price: 11.9, art: "lemon", icon: "fa-lemon" },
  { id: "galleta", name: "Galleta", category: "Especiales", price: 14.5, art: "cookie", icon: "fa-cookie" },
  { id: "arandano", name: "Arándano", category: "Frutales", price: 14.2, art: "blueberry", icon: "fa-apple-whole" },
  { id: "caramelo", name: "Caramelo", category: "Especiales", price: 14.9, art: "caramel", icon: "fa-candy-cane" }
];
const $ = (selector) => document.querySelector(selector);
const productGrid = $("#productGrid");
const cartItems = $("#cartItems");
const defaultIngredients = [
  ["leche", "Leche", "ml", 10000], ["crema", "Crema", "ml", 5000], ["azucar", "Azúcar", "g", 5000],
  ["vainilla", "Esencia de vainilla", "ml", 500], ["fresa", "Fresa", "g", 3000], ["menta", "Menta", "g", 300],
  ["chocolate", "Chocolate", "g", 3000], ["mango", "Mango", "g", 3000], ["limon", "Limón", "g", 2000],
  ["galleta", "Galleta", "g", 3000], ["arandano", "Arándano", "g", 2000], ["caramelo", "Caramelo", "g", 2000]
];
const defaultRecipes = {
  vainilla: { leche: 100, crema: 40, azucar: 25, vainilla: 3 },
  fresa: { leche: 80, crema: 40, azucar: 25, fresa: 40 },
  "mint-oreo": { leche: 90, crema: 40, azucar: 25, menta: 2, galleta: 20 },
  chocolate: { leche: 90, crema: 40, azucar: 20, chocolate: 35 },
  mango: { leche: 80, crema: 40, azucar: 20, mango: 45 },
  limon: { leche: 90, crema: 30, azucar: 25, limon: 30 },
  galleta: { leche: 90, crema: 40, azucar: 20, galleta: 35 },
  arandano: { leche: 80, crema: 40, azucar: 25, arandano: 35 },
  caramelo: { leche: 90, crema: 40, azucar: 20, caramelo: 30 }
};
let state;
let activeView = "catalogView";
let selectedCardId = "";
let storageError = false;
const cart = new Map();

function formatMoney(value) {
  return money.format(value);
}

function escapeHtml(value) {
  return String(value).replace(/[&<>"']/g, (char) => ({
    "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;"
  })[char]);
}

function localDateKey(date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
}

function newInitialState() {
  const now = new Date().toISOString();
  const ingredients = defaultIngredients.map(([id, name, unit, quantity]) => ({ id, name, unit, quantity }));
  const movements = ingredients.map((ingredient) => ({
    id: crypto.randomUUID(), createdAt: now, ingredientId: ingredient.id, ingredientName: ingredient.name,
    type: "entrada", quantity: ingredient.quantity, note: "Inventario inicial", source: "manual"
  }));
  return {
    sales: [],
    ingredients,
    recipes: JSON.parse(JSON.stringify(defaultRecipes)),
    movements,
    cards: []
  };
}

function loadState() {
  try {
    const saved = localStorage.getItem(APP_STORAGE_KEY);
    if (saved !== null) {
      const parsed = JSON.parse(saved);
      if (!parsed || !Array.isArray(parsed.sales) || !Array.isArray(parsed.ingredients) ||
          !parsed.recipes || !Array.isArray(parsed.movements) || !Array.isArray(parsed.cards) ||
          parsed.sales.some((sale) => !sale || typeof sale.id !== "string" ||
            !Number.isFinite(Date.parse(sale.createdAt)) || !Array.isArray(sale.items) || !Number.isFinite(sale.total)) ||
          parsed.ingredients.some((ingredient) => !ingredient || typeof ingredient.id !== "string" ||
            typeof ingredient.name !== "string" || typeof ingredient.unit !== "string" ||
            !Number.isFinite(ingredient.quantity) || ingredient.quantity < 0)) {
        throw new Error("Los datos guardados no tienen un formato válido.");
      }
      return parsed;
    }
    const initial = newInitialState();
    const legacy = localStorage.getItem(LEGACY_SALES_KEY);
    if (legacy !== null) {
      const sales = JSON.parse(legacy);
      if (!Array.isArray(sales) || sales.some((sale) => !sale || typeof sale.id !== "string" ||
          !Number.isFinite(Date.parse(sale.createdAt)) || !Array.isArray(sale.items) || !Number.isFinite(sale.total))) {
        throw new Error("El historial de ventas existente no es válido.");
      }
      initial.sales = sales;
    }
    return initial;
  } catch (error) {
    storageError = true;
    showNotice(`No se pudieron leer los datos guardados: ${error.message}`, "error");
    return newInitialState();
  }
}

function commit(nextState, successMessage = "") {
  if (storageError) {
    showNotice("No se guardaron los cambios porque los datos locales tienen un error de lectura. No cierres ni recargues la página antes de revisar el almacenamiento.", "error");
    return false;
  }
  try {
    localStorage.setItem(APP_STORAGE_KEY, JSON.stringify(nextState));
  } catch (error) {
    showNotice(`No se guardaron los cambios: ${error.message}. Libera espacio o revisa el almacenamiento del navegador.`, "error");
    return false;
  }
  state = nextState;
  if (successMessage) showNotice(successMessage, "success");
  renderAll();
  return true;
}

function showNotice(message, type) {
  if (window.Swal) {
    window.Swal.mixin({
      toast: true, position: "top-end", showConfirmButton: false,
      timer: type === "error" ? 5500 : 3000, timerProgressBar: true
    }).fire({ icon: type === "error" ? "error" : "success", title: message });
    return;
  }
  const notice = $("#notice");
  notice.textContent = message;
  notice.className = `notice show ${type}`;
}

function getIngredient(id, source = state) {
  return source.ingredients.find((ingredient) => ingredient.id === id);
}

function productRecipe(productId, source = state) {
  return source.recipes[productId] || {};
}

function canMakeProduct(productId, quantity = 1, source = state) {
  const recipe = productRecipe(productId, source);
  const recipeEntries = Object.entries(recipe);
  if (!recipeEntries.length) return false;
  return recipeEntries.every(([ingredientId, amount]) => {
    const ingredient = getIngredient(ingredientId, source);
    return ingredient && ingredient.quantity + 1e-8 >= amount * quantity;
  });
}

function availableServings(productId) {
  const recipe = productRecipe(productId);
  const entries = Object.entries(recipe);
  if (!entries.length) return 0;
  return Math.max(0, Math.min(...entries.map(([ingredientId, amount]) => {
    const ingredient = getIngredient(ingredientId);
    return ingredient && amount > 0 ? Math.floor((ingredient.quantity + 1e-8) / amount) : 0;
  })));
}

function getTotal() {
  return Math.round([...cart.entries()].reduce((sum, [id, quantity]) => {
    const product = products.find((item) => item.id === id);
    return sum + (product ? product.price * quantity : 0);
  }, 0) * 100) / 100;
}

function projectedRecipeNeeds(extraProductId = "", extraQuantity = 0) {
  const needs = new Map();
  for (const [productId, cartQuantity] of cart) {
    const quantity = cartQuantity + (productId === extraProductId ? extraQuantity : 0);
    for (const [ingredientId, perServing] of Object.entries(productRecipe(productId))) {
      needs.set(ingredientId, (needs.get(ingredientId) || 0) + perServing * quantity);
    }
  }
  if (extraProductId && !cart.has(extraProductId)) {
    for (const [ingredientId, perServing] of Object.entries(productRecipe(extraProductId))) {
      needs.set(ingredientId, (needs.get(ingredientId) || 0) + perServing * extraQuantity);
    }
  }
  return needs;
}

function canAddToCart(productId, quantity = 1) {
  const needs = projectedRecipeNeeds(productId, quantity);
  return [...needs].every(([ingredientId, amount]) => {
    const ingredient = getIngredient(ingredientId);
    return ingredient && ingredient.quantity + 1e-8 >= amount;
  });
}

function renderCategories() {
  const names = ["Todos", ...new Set(products.map((product) => product.category))];
  $("#categories").innerHTML = names.map((category) =>
    `<button type="button" class="category btn${category === categoryFilter ? " active" : ""}" data-category="${escapeHtml(category)}" aria-pressed="${category === categoryFilter}">${escapeHtml(category)}</button>`
  ).join("");
}

let categoryFilter = "Todos";
let searchFilter = "";

function renderProducts() {
  const query = searchFilter.trim().toLocaleLowerCase("es");
  const visible = products.filter((product) =>
    (categoryFilter === "Todos" || product.category === categoryFilter) &&
    (!query || product.name.toLocaleLowerCase("es").includes(query))
  );
  productGrid.innerHTML = visible.length ? visible.map((product) => {
    const available = availableServings(product.id);
    return `<button class="product-card btn text-start" type="button" data-product="${product.id}" ${available === 0 ? "disabled" : ""} aria-label="Agregar ${escapeHtml(product.name)}, ${formatMoney(product.price)}">
      <span class="product-art art-${product.art}" aria-hidden="true"><i class="fa-solid ${product.icon}"></i></span>
      <span class="product-name">${escapeHtml(product.name)}</span>
      <span class="product-category">${escapeHtml(product.category)}</span>
      <span class="product-price">${formatMoney(product.price)}</span>
      <span class="product-stock">${available ? `${available} porciones disponibles` : "Sin existencias"}</span>
    </button>`;
  }).join("") : '<div class="empty-products">No encontramos productos con esa búsqueda.</div>';
}

function renderCart() {
  const entries = [...cart.entries()];
  const quantity = entries.reduce((sum, [, count]) => sum + count, 0);
  const total = getTotal();
  $("#itemCount").textContent = String(quantity);
  $("#orderCaption").textContent = quantity ? `${quantity} ${quantity === 1 ? "artículo" : "artículos"} en la orden` : "Añade productos para comenzar";
  cartItems.innerHTML = entries.length ? entries.map(([id, count]) => {
    const product = products.find((item) => item.id === id);
    return `<div class="cart-line">
      <div><span class="cart-name">${escapeHtml(product.name)}</span><span class="cart-unit-price">${formatMoney(product.price)} c/u</span>
        <div class="cart-line-actions">
          <button class="qty-button" type="button" data-action="decrease" data-id="${id}" aria-label="Quitar una unidad de ${escapeHtml(product.name)}"><i class="fa-solid fa-minus" aria-hidden="true"></i></button>
          <span class="quantity">${count}</span>
          <button class="qty-button" type="button" data-action="increase" data-id="${id}" aria-label="Agregar una unidad de ${escapeHtml(product.name)}"><i class="fa-solid fa-plus" aria-hidden="true"></i></button>
          <button class="remove-button" type="button" data-action="remove" data-id="${id}" aria-label="Eliminar ${escapeHtml(product.name)}"><i class="fa-solid fa-trash-can" aria-hidden="true"></i></button>
        </div>
      </div><span class="cart-line-total">${formatMoney(product.price * count)}</span>
    </div>`;
  }).join("") : '<div class="empty-cart"><span class="empty-icon" aria-hidden="true"><i class="fa-solid fa-basket-shopping"></i></span>Tu orden está vacía.<br>Agrega un sabor para comenzar.</div>';
  $("#subtotal").textContent = formatMoney(total);
  $("#total").textContent = formatMoney(total);
  $("#checkout").innerHTML = `<i class="fa-solid fa-cash-register me-2" aria-hidden="true"></i>Cobrar ${formatMoney(total)}`;
  $("#clearCart").disabled = !entries.length;
  updatePayment();
}

function updatePayment() {
  const payment = $('input[name="payment"]:checked').value;
  const total = getTotal();
  const cashReceived = Number($("#cashReceived").value);
  const isCash = payment === "efectivo";
  const isCard = payment === "tarjeta";
  const hasValidCash = !isCash || (Number.isFinite(cashReceived) && cashReceived >= total && total > 0);
  $("#cashRow").hidden = !isCash;
  $("#cardRow").hidden = !isCard;
  $("#changeRow").hidden = !isCash || total <= 0 || !Number.isFinite(cashReceived) || cashReceived < total;
  $("#changeAmount").textContent = formatMoney(Math.max(0, cashReceived - total));
  $("#checkout").disabled = !cart.size || !hasValidCash || (isCard && !state.cards.some((card) => card.id === selectedCardId)) || storageError;
}

function addProduct(id) {
  if (!canAddToCart(id)) {
    showNotice("No hay ingredientes suficientes para agregar otra porción.", "error");
    return;
  }
  cart.set(id, (cart.get(id) || 0) + 1);
  renderProducts();
  renderCart();
}

function updateCartItem(action, id) {
  const current = cart.get(id);
  if (current === undefined) return;
  if (action === "remove" || (action === "decrease" && current <= 1)) cart.delete(id);
  else if (action === "decrease") cart.set(id, current - 1);
  else if (action === "increase" && canAddToCart(id)) cart.set(id, current + 1);
  else if (action === "increase") showNotice("No hay ingredientes suficientes para esa cantidad.", "error");
  renderProducts();
  renderCart();
}

function loadSelectOptions() {
  const previousRecipeIngredient = $("#recipeIngredient").value;
  const previousMovementIngredient = $("#movementIngredient").value;
  $("#recipeProduct").innerHTML = products.map((product) => `<option value="${product.id}">${escapeHtml(product.name)}</option>`).join("");
  const options = state.ingredients.map((ingredient) => `<option value="${escapeHtml(ingredient.id)}">${escapeHtml(ingredient.name)} (${ingredient.unit})</option>`).join("");
  $("#recipeIngredient").innerHTML = options || '<option value="">Añade primero un ingrediente</option>';
  $("#movementIngredient").innerHTML = options || '<option value="">Añade primero un ingrediente</option>';
  if (state.ingredients.some((ingredient) => ingredient.id === previousRecipeIngredient)) $("#recipeIngredient").value = previousRecipeIngredient;
  if (state.ingredients.some((ingredient) => ingredient.id === previousMovementIngredient)) $("#movementIngredient").value = previousMovementIngredient;
  const oldCard = selectedCardId;
  $("#savedCards").innerHTML = state.cards.length
    ? state.cards.map((card) => `<option value="${escapeHtml(card.id)}">${escapeHtml(card.brand)} ···· ${escapeHtml(card.last4)} (${escapeHtml(card.holder)})</option>`).join("")
    : '<option value="">Añade una tarjeta para continuar</option>';
  if (state.cards.some((card) => card.id === oldCard)) $("#savedCards").value = oldCard;
  else selectedCardId = state.cards[0]?.id || "";
  updatePayment();
}

function renderInventory() {
  const lowStock = state.ingredients.filter((ingredient) => ingredient.quantity < 500).length;
  $("#inventorySummary").innerHTML = `<article class="summary-card"><span class="summary-label">Ingredientes registrados</span><strong class="summary-value">${state.ingredients.length}</strong></article>
    <article class="summary-card"><span class="summary-label">Existencias bajas (menos de 500)</span><strong class="summary-value">${lowStock}</strong></article>`;
  $("#inventoryRows").innerHTML = state.ingredients.length ? state.ingredients.map((ingredient) => {
    const low = ingredient.quantity < 500;
    return `<tr><td><strong>${escapeHtml(ingredient.name)}</strong>${low ? '<span class="stock-warning"><i class="fa-solid fa-triangle-exclamation" aria-hidden="true"></i> Bajo</span>' : ""}</td>
      <td>${Number(ingredient.quantity.toFixed(2))}</td><td>${escapeHtml(ingredient.unit)}</td>
      <td><button class="quiet-button stock-action" type="button" data-stock="entrada" data-id="${escapeHtml(ingredient.id)}" aria-label="Añadir stock de ${escapeHtml(ingredient.name)}"><i class="fa-solid fa-plus" aria-hidden="true"></i> Entrada</button>
      <button class="quiet-button stock-action" type="button" data-stock="salida" data-id="${escapeHtml(ingredient.id)}" aria-label="Registrar salida de ${escapeHtml(ingredient.name)}"><i class="fa-solid fa-minus" aria-hidden="true"></i> Salida</button></td></tr>`;
  }).join("") : '<tr><td colspan="4" class="empty-table">Todavía no hay ingredientes registrados.</td></tr>';
}

function renderRecipes() {
  $("#recipeList").innerHTML = products.map((product) => {
    const recipe = productRecipe(product.id);
    const rows = Object.entries(recipe).map(([ingredientId, amount]) => {
      const ingredient = getIngredient(ingredientId);
      if (!ingredient) return "";
      return `<li><span>${escapeHtml(ingredient.name)}</span><strong>${Number(amount.toFixed(2))} ${escapeHtml(ingredient.unit)}</strong>
        <button type="button" class="remove-button" data-remove-recipe="${product.id}" data-ingredient="${escapeHtml(ingredientId)}" aria-label="Quitar ${escapeHtml(ingredient.name)} de receta"><i class="fa-solid fa-trash-can" aria-hidden="true"></i></button></li>`;
    }).join("");
    return `<article class="recipe-card"><h2><i class="fa-solid fa-ice-cream me-2" aria-hidden="true"></i>${escapeHtml(product.name)}</h2>
      <p>${formatMoney(product.price)} · ${availableServings(product.id)} porciones disponibles</p>
      ${rows ? `<ul>${rows}</ul>` : '<p class="text-muted">Sin ingredientes definidos; no se puede vender este sabor.</p>'}</article>`;
  }).join("");
}

function renderMovements() {
  $("#movementRows").innerHTML = state.movements.length ? [...state.movements].reverse().map((movement) => {
    const isIn = movement.type === "entrada";
    return `<tr><td>${escapeHtml(new Date(movement.createdAt).toLocaleString("es-MX"))}</td><td>${escapeHtml(movement.ingredientName)}</td>
      <td><span class="movement-badge ${isIn ? "in" : "out"}"><i class="fa-solid ${isIn ? "fa-arrow-down" : "fa-arrow-up"}" aria-hidden="true"></i> ${isIn ? "Entrada" : "Salida"}</span></td>
      <td>${isIn ? "+" : "−"}${Number(movement.quantity.toFixed(2))}</td><td>${escapeHtml(movement.note || "")}</td></tr>`;
  }).join("") : '<tr><td colspan="5" class="empty-table">Aún no hay movimientos de inventario.</td></tr>';
}

function renderSales() {
  const todaySales = state.sales.filter((sale) => localDateKey(new Date(sale.createdAt)) === localDateKey(new Date()));
  $("#todayCount").textContent = String(todaySales.length);
  $("#todayTotal").textContent = formatMoney(todaySales.reduce((sum, sale) => sum + sale.total, 0));
  $("#salesList").innerHTML = state.sales.length ? [...state.sales].reverse().map((sale) => {
    const units = sale.items.reduce((sum, item) => sum + item.quantity, 0);
    return `<article class="sale-row"><div><div class="sale-id">${escapeHtml(sale.id)}${sale.customer ? ` · ${escapeHtml(sale.customer)}` : ""}</div>
      <div class="sale-date">${escapeHtml(new Date(sale.createdAt).toLocaleString("es-MX"))}</div>
      <div class="sale-meta">${units} ${units === 1 ? "artículo" : "artículos"} · ${sale.payment === "efectivo" ? "Efectivo" : "Tarjeta"}</div></div>
      <span class="sale-total">${formatMoney(sale.total)}</span>
      <button type="button" class="sale-receipt btn" data-receipt="${escapeHtml(sale.id)}"><i class="fa-solid fa-receipt me-1" aria-hidden="true"></i>Ver ticket</button></article>`;
  }).join("") : '<div class="empty-sales">Todavía no hay ventas registradas en este dispositivo.</div>';
}

function renderAll() {
  renderCategories();
  renderProducts();
  renderCart();
  loadSelectOptions();
  renderInventory();
  renderRecipes();
  renderMovements();
  if (activeView === "salesView") renderSales();
}

function recordMovement(next, ingredient, type, quantity, note, source = "manual") {
  const movement = {
    id: crypto.randomUUID(), createdAt: new Date().toISOString(), ingredientId: ingredient.id,
    ingredientName: ingredient.name, type, quantity, note, source
  };
  next.movements.push(movement);
  return movement;
}

async function saveSale() {
  if (!cart.size || storageError) return;
  const total = getTotal();
  const payment = $('input[name="payment"]:checked').value;
  const received = payment === "efectivo" ? Number($("#cashReceived").value) : total;
  if (payment === "efectivo" && (!Number.isFinite(received) || received < total)) {
    showNotice("El efectivo recibido debe ser igual o mayor al total.", "error");
    return;
  }
  const card = payment === "tarjeta" ? state.cards.find((item) => item.id === selectedCardId) : null;
  if (payment === "tarjeta" && !card) {
    showNotice("Añade o selecciona una tarjeta para continuar.", "error");
    return;
  }
  const needed = projectedRecipeNeeds();
  if (![...needed].every(([id, amount]) => getIngredient(id).quantity + 1e-8 >= amount)) {
    showNotice("El stock cambió y ya no alcanza para esta orden. Revisa los ingredientes.", "error");
    renderAll();
    return;
  }
  const next = structuredClone(state);
  const sale = {
    id: `A-${Date.now().toString(36).toUpperCase()}`,
    createdAt: new Date().toISOString(),
    customer: $("#customer").value.trim(),
    payment,
    received,
    change: payment === "efectivo" ? Math.round((received - total) * 100) / 100 : 0,
    card: card ? { brand: card.brand, last4: card.last4 } : null,
    items: [...cart.entries()].map(([id, quantity]) => {
      const product = products.find((item) => item.id === id);
      return { id, name: product.name, price: product.price, quantity, subtotal: product.price * quantity };
    }),
    total
  };
  for (const [ingredientId, amount] of needed) {
    const ingredient = getIngredient(ingredientId, next);
    ingredient.quantity = Math.round((ingredient.quantity - amount) * 100) / 100;
    recordMovement(next, ingredient, "salida", amount, `Consumo por venta ${sale.id}`, "venta");
  }
  next.sales.push(sale);
  if (!commit(next)) return;
  cart.clear();
  $("#customer").value = "";
  $("#cashReceived").value = "";
  $('input[name="payment"][value="efectivo"]').checked = true;
  renderAll();
  openReceipt(sale);
}

function openReceipt(sale) {
  $("#receiptContent").innerHTML = `<div class="receipt">
    <div class="receipt-brand"><span class="brand-mark" aria-hidden="true"><i class="fa-solid fa-ice-cream"></i></span><h2 id="receiptTitle">Heladería Aurora</h2><p>Ticket de venta</p></div>
    <div class="receipt-info"><span><strong>Folio</strong><br>${escapeHtml(sale.id)}</span><span class="receipt-date"><strong>Fecha</strong><br>${escapeHtml(new Date(sale.createdAt).toLocaleString("es-MX"))}</span></div>
    ${sale.customer ? `<p class="receipt-info"><span>Cliente</span><strong>${escapeHtml(sale.customer)}</strong></p>` : ""}
    <div class="receipt-items">${sale.items.map((item) => `<div class="receipt-item"><span>${item.quantity} × ${escapeHtml(item.name)}<br><small>${formatMoney(item.price)} c/u</small></span><strong>${formatMoney(item.subtotal)}</strong></div>`).join("")}</div>
    <div class="receipt-totals"><div class="total-row"><span>Subtotal</span><strong>${formatMoney(sale.total)}</strong></div>
      <div class="total-row grand"><span>Total</span><strong>${formatMoney(sale.total)}</strong></div>
      <div class="total-row"><span>Pago (${sale.payment === "efectivo" ? "efectivo" : "tarjeta"})</span><span>${formatMoney(sale.received)}</span></div>
      ${sale.payment === "efectivo" ? `<div class="total-row"><span>Cambio</span><span>${formatMoney(sale.change)}</span></div>` : `<div class="total-row"><span>Tarjeta</span><span>${escapeHtml(sale.card?.brand || "Tarjeta")} ···· ${escapeHtml(sale.card?.last4 || "")}</span></div>`}
    </div><p class="receipt-thanks">¡Gracias por tu compra!</p>
  </div>`;
  $("#receiptDialog").showModal();
}

function setView(view) {
  activeView = view;
  $("#catalogView").hidden = view !== "catalogView";
  $("#salesView").classList.toggle("visible", view === "salesView");
  $("#salesView").hidden = view !== "salesView";
  document.querySelectorAll(".module-view").forEach((section) => { section.hidden = section.id !== view; });
  $("#orderPanel").hidden = !["catalogView", "salesView"].includes(view);
  document.querySelectorAll(".nav-tab").forEach((button) => button.classList.toggle("active", button.dataset.view === (view === "salesView" ? "catalogView" : view)));
  if (view === "salesView") renderSales();
}

async function addCard() {
  if (!window.Swal) {
    showNotice("SweetAlert no está disponible para registrar una tarjeta.", "error");
    return;
  }
  const result = await window.Swal.fire({
    title: "Añadir tarjeta",
    html: '<input id="cardHolder" class="swal2-input" maxlength="60" placeholder="Nombre del titular" autocomplete="off"><select id="cardBrand" class="swal2-select"><option value="Visa">Visa</option><option value="Mastercard">Mastercard</option><option value="American Express">American Express</option><option value="Otra">Otra</option></select><input id="cardLast4" class="swal2-input" inputmode="numeric" maxlength="4" placeholder="Últimos 4 dígitos" autocomplete="off">',
    focusConfirm: false,
    showCancelButton: true,
    confirmButtonText: "Guardar tarjeta",
    cancelButtonText: "Cancelar",
    confirmButtonColor: "#db5d87",
    preConfirm: () => {
      const popup = window.Swal.getPopup();
      const holder = popup.querySelector("#cardHolder").value.trim();
      const brand = popup.querySelector("#cardBrand").value;
      const last4 = popup.querySelector("#cardLast4").value.trim();
      if (!holder || !/^\d{4}$/.test(last4)) {
        window.Swal.showValidationMessage("Ingresa el titular y exactamente los últimos 4 dígitos.");
        return false;
      }
      return { holder, brand, last4 };
    }
  });
  if (!result.isConfirmed) return;
  const next = structuredClone(state);
  const card = { id: crypto.randomUUID(), ...result.value };
  next.cards.push(card);
  if (commit(next, "Tarjeta añadida.")) {
    selectedCardId = card.id;
    $("#savedCards").value = card.id;
    updatePayment();
  }
}

function addIngredient(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const name = form.elements.name.value.trim();
  const unit = form.elements.unit.value;
  const quantity = Number(form.elements.quantity.value);
  if (!name || !Number.isFinite(quantity) || quantity < 0) return;
  if (state.ingredients.some((ingredient) => ingredient.name.toLocaleLowerCase("es") === name.toLocaleLowerCase("es"))) {
    showNotice("Ya existe un ingrediente con ese nombre.", "error");
    return;
  }
  const id = `ing-${crypto.randomUUID()}`;
  const next = structuredClone(state);
  const ingredient = { id, name, unit, quantity };
  next.ingredients.push(ingredient);
  if (quantity > 0) recordMovement(next, ingredient, "entrada", quantity, "Existencia inicial", "manual");
  if (commit(next, "Ingrediente añadido.")) form.reset();
}

function addRecipeIngredient(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const productId = form.elements.product.value;
  const ingredientId = form.elements.ingredient.value;
  const quantity = Number(form.elements.quantity.value);
  if (!getIngredient(ingredientId) || !Number.isFinite(quantity) || quantity <= 0) return;
  const next = structuredClone(state);
  if (!next.recipes[productId]) next.recipes[productId] = {};
  next.recipes[productId][ingredientId] = quantity;
  if (commit(next, "Receta actualizada.")) form.elements.quantity.value = "";
}

function addStockMovement(event) {
  event.preventDefault();
  const form = event.currentTarget;
  const ingredient = getIngredient(form.elements.ingredient.value);
  const type = form.elements.type.value;
  const quantity = Number(form.elements.quantity.value);
  const note = form.elements.note.value.trim() || (type === "entrada" ? "Entrada de stock" : "Salida manual");
  if (!ingredient || !Number.isFinite(quantity) || quantity <= 0) return;
  if (type === "salida" && ingredient.quantity < quantity) {
    showNotice(`No hay suficiente stock de ${ingredient.name}.`, "error");
    return;
  }
  const next = structuredClone(state);
  const target = getIngredient(ingredient.id, next);
  target.quantity = Math.round((target.quantity + (type === "entrada" ? quantity : -quantity)) * 100) / 100;
  recordMovement(next, target, type, quantity, note);
  if (commit(next, "Movimiento de inventario registrado.")) form.reset();
}

async function quickStockMovement(type, ingredientId) {
  const ingredient = getIngredient(ingredientId);
  if (!ingredient || !window.Swal) return;
  const result = await window.Swal.fire({
    title: `${type === "entrada" ? "Añadir" : "Registrar salida"} · ${escapeHtml(ingredient.name)}`,
    input: "number", inputAttributes: { min: "0.01", step: "0.01" }, inputLabel: `Cantidad (${ingredient.unit})`,
    showCancelButton: true, confirmButtonText: "Continuar", cancelButtonText: "Cancelar", confirmButtonColor: "#db5d87",
    inputValidator: (value) => !Number.isFinite(Number(value)) || Number(value) <= 0 ? "Escribe una cantidad válida." : undefined
  });
  if (!result.isConfirmed) return;
  const quantity = Number(result.value);
  if (type === "salida" && ingredient.quantity < quantity) {
    showNotice(`No hay suficiente stock de ${ingredient.name}.`, "error");
    return;
  }
  const next = structuredClone(state);
  const target = getIngredient(ingredientId, next);
  target.quantity = Math.round((target.quantity + (type === "entrada" ? quantity : -quantity)) * 100) / 100;
  recordMovement(next, target, type, quantity, type === "entrada" ? "Entrada de stock" : "Salida manual");
  commit(next, "Stock actualizado.");
}

$("#categories").addEventListener("click", (event) => {
  const button = event.target.closest("[data-category]");
  if (!button) return;
  categoryFilter = button.dataset.category;
  renderCategories();
  renderProducts();
});
productGrid.addEventListener("click", (event) => {
  const button = event.target.closest("[data-product]");
  if (button && !button.disabled) addProduct(button.dataset.product);
});
cartItems.addEventListener("click", (event) => {
  const button = event.target.closest("[data-action]");
  if (button) updateCartItem(button.dataset.action, button.dataset.id);
});
$("#search").addEventListener("input", (event) => {
  searchFilter = event.target.value;
  renderProducts();
});
document.querySelectorAll('input[name="payment"]').forEach((input) => input.addEventListener("change", updatePayment));
$("#cashReceived").addEventListener("input", updatePayment);
$("#savedCards").addEventListener("change", (event) => { selectedCardId = event.target.value; updatePayment(); });
$("#addCard").addEventListener("click", addCard);
$("#checkout").addEventListener("click", saveSale);
$("#clearCart").addEventListener("click", async () => {
  const result = window.Swal ? await window.Swal.fire({
    title: "¿Vaciar la orden?", text: "Se quitarán todos los productos de la venta actual.", icon: "warning",
    showCancelButton: true, confirmButtonText: "Sí, vaciar", cancelButtonText: "Cancelar", confirmButtonColor: "#db5d87"
  }) : { isConfirmed: window.confirm("¿Vaciar todos los productos de la orden actual?") };
  if (!result.isConfirmed) return;
  cart.clear();
  $("#cashReceived").value = "";
  renderAll();
});
$("#showSales").addEventListener("click", () => setView("salesView"));
$("#backToSale").addEventListener("click", () => setView("catalogView"));
document.querySelectorAll(".nav-tab").forEach((button) => button.addEventListener("click", () => setView(button.dataset.view)));
$("#salesList").addEventListener("click", (event) => {
  const button = event.target.closest("[data-receipt]");
  const sale = state.sales.find((item) => item.id === button?.dataset.receipt);
  if (sale) openReceipt(sale);
});
$("#ingredientForm").addEventListener("submit", addIngredient);
$("#recipeForm").addEventListener("submit", addRecipeIngredient);
$("#movementForm").addEventListener("submit", addStockMovement);
$("#inventoryRows").addEventListener("click", (event) => {
  const button = event.target.closest("[data-stock]");
  if (button) quickStockMovement(button.dataset.stock, button.dataset.id);
});
$("#recipeList").addEventListener("click", (event) => {
  const button = event.target.closest("[data-remove-recipe]");
  if (!button) return;
  const next = structuredClone(state);
  delete next.recipes[button.dataset.removeRecipe][button.dataset.ingredient];
  commit(next, "Ingrediente eliminado de la receta.");
});
$("#closeReceipt").addEventListener("click", () => $("#receiptDialog").close());
$("#receiptDialog").addEventListener("click", (event) => {
  if (event.target === $("#receiptDialog")) $("#receiptDialog").close();
});
$("#printReceipt").addEventListener("click", () => window.print());

$("#headerDate").textContent = new Intl.DateTimeFormat("es-MX", { dateStyle: "medium" }).format(new Date());
state = loadState();
renderAll();
