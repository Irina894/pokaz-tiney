const API = "http://localhost:5289/api";

// ---------- користувач ----------
function currentUser() {
  try { return JSON.parse(localStorage.getItem("user") || sessionStorage.getItem("user") || "null"); }
  catch { return null; }
}
function logout() {
  localStorage.removeItem("user"); sessionStorage.removeItem("user");
  location.href = "index.html";
}
const USER = currentUser();
const USER_ID = USER ? USER.id : null;

// ---------- запити ----------
async function api(path, options) {
  const res = await fetch(API + path, {
    ...options,
    headers: options && options.body ? { "Content-Type": "application/json" } : undefined,
  });
  if (!res.ok) {
    let detail = "";
    try { detail = (await res.text()).slice(0, 200); } catch {}
    const err = new Error(`Помилка ${res.status}${detail ? ": " + detail : ""}`);
    err.status = res.status; err.detail = detail;
    throw err;
  }
  return res.status === 204 ? null : res.json().catch(() => null);
}

// ---------- форматування ----------
const esc = s => String(s ?? "").replace(/[&<>"']/g, c =>
  ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
const fmtTime = d => new Date(d).toLocaleTimeString("uk-UA", { hour: "2-digit", minute: "2-digit" });
const fmtDate = d => new Date(d).toLocaleDateString("uk-UA", { day: "numeric", month: "long" });
const fmtDateFull = d => new Date(d).toLocaleDateString("uk-UA", { day: "numeric", month: "long", year: "numeric" }).replace(/\s?р\.$/, "");
const dayKey = d => { const x = new Date(d); return `${x.getFullYear()}-${String(x.getMonth() + 1).padStart(2, "0")}-${String(x.getDate()).padStart(2, "0")}`; };
const price = p => `${Number(p).toFixed(0)} грн`;
function durText(min) {
  const h = Math.floor(min / 60), m = min % 60;
  return (h ? `${h} год` : "") + (h && m ? " " : "") + (m ? `${m} хв` : "");
}
const stars = n => "★".repeat(n) + "☆".repeat(5 - n);

const CLOCK = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="#f5c518" stroke-width="2"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>';
const SEARCH = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M20 20l-4-4"/></svg>';

function toast(text) {
  let t = document.getElementById("toast");
  if (!t) { t = document.createElement("div"); t.id = "toast"; document.body.appendChild(t); }
  t.textContent = text;
  t.classList.add("show");
  clearTimeout(toast._t);
  toast._t = setTimeout(() => t.classList.remove("show"), 2800);
}

function posterHtml(m) {
  return m.posterUrl
    ? `<img src="${esc(m.posterUrl)}" alt="${esc(m.title)}" loading="lazy">`
    : esc((m.title || "?").charAt(0));
}

function showError(el, e) {
  el.innerHTML = `<p class="msg err">Не вдалося завантажити дані. Перевірте, що API запущено (${esc(API)}). ${esc(e.message)}</p>`;
}

function ticketStatus(s) {
  const v = typeof s === "string" ? s.toLowerCase() : ["pending", "paid", "cancelled"][s];
  return {
    pending:   { cls: "",          text: "Очікує оплати" },
    paid:      { cls: "paid",      text: "Оплачено" },
    cancelled: { cls: "cancelled", text: "Скасовано" },
  }[v] || { cls: "", text: String(s) };
}
const isCancelled = s => ticketStatus(s).cls === "cancelled";

// ---------- спільні шматки сторінок ----------
function metaHtml(m) {
  return `${m.rating != null ? `<span class="rate"><b class="imdb">IMDb</b> ${esc(m.rating)}<small>/10</small></span>` : ""}
          <span>${CLOCK} ${durText(m.durationMinutes)}</span>`;
}

function filmCard(m, soon) {
  return `<a class="film-card" href="film.html?id=${m.id}">
    <div class="pic">${posterHtml(m)}
      <div class="info">
        <span>${m.rating != null ? `<b class="imdb">IMDb</b>${esc(m.rating)}` : ""}</span>
        <span>${CLOCK} ${durText(m.durationMinutes)}</span>
        ${soon && m.releaseDate ? `<span class="date">${fmtDate(m.releaseDate)}</span>` : ""}
      </div>
    </div>
    <div class="name">${esc(m.title)}</div>
  </a>`;
}

function wireRail(root) {
  root.querySelectorAll(".railwrap").forEach(w => {
    const r = w.querySelector(".rail");
    w.querySelector(".l").onclick = () => r.scrollBy({ left: -r.clientWidth, behavior: "smooth" });
    w.querySelector(".r").onclick = () => r.scrollBy({ left: r.clientWidth, behavior: "smooth" });
    const upd = () => w.classList.toggle("noscroll", r.scrollWidth <= r.clientWidth + 2);
    upd();
    addEventListener("resize", upd);
  });
}

function header(active) {
  const nav = (href, text, key) => `<a href="${href}" class="${active === key ? "active" : ""}">${text}</a>`;
  const left = USER
    ? `<a class="avatar" href="tickets.html" title="Мої квитки">${esc((USER.name || "?").charAt(0).toUpperCase())}</a>
       <button class="btn outline small" onclick="logout()">Вийти</button>`
    : `<a class="btn small" href="login.html?tab=register">Реєстрація</a>
       <a class="btn outline small" href="login.html">Вхід</a>`;
  document.getElementById("header").innerHTML = `
    <header class="top"><div class="wrap">
      <div class="auth">${left}</div>
      <form class="hsearch" role="search" onsubmit="event.preventDefault();location.href='catalog.html?q='+encodeURIComponent(this.q.value.trim())">
        <input name="q" placeholder="пошук ..." aria-label="Пошук фільмів">
        <button aria-label="Шукати">${SEARCH}</button>
      </form>
      <nav>${nav("catalog.html", "Зараз у прокаті", "films")}${nav("tickets.html", "Мої квитки", "tickets")}${nav("index.html", "Головна сторінка", "home")}</nav>
      <span class="hdiv"></span>
      <a class="logo" href="index.html"><img src="images/logo.png" alt="" onerror="this.remove()"><span>Показ <em>тіней</em></span></a>
    </div></header>`;
}

function footer() {
  const el = document.getElementById("footer");
  if (!el) return;
  el.innerHTML = `<footer class="big"><div class="wrap">
    <div class="foot4">
      <div><div class="logo" style="margin-bottom:12px">Показ <em>тіней</em></div><p>Кіно просто неба та в незвичних локаціях.</p></div>
      <div><h4>Контакти</h4><ul><li>+38 (0XX) XXX XX XX</li><li>info@pokaz-tinei.ua</li><li>м. Чернівці</li></ul></div>
      <div><h4>Навігація</h4><ul>
        <li><a href="catalog.html">Зараз у прокаті</a></li><li><a href="tickets.html">Мої квитки</a></li><li><a href="index.html">Головна сторінка</a></li></ul></div>
      <div><h4>Соціальні мережі</h4><ul><li>Facebook</li><li>Instagram</li><li>Telegram</li><li>YouTube</li></ul></div>
    </div>
    <div class="copy">© Показ тіней. Навчальний проєкт.</div>
  </div></footer>`;
}