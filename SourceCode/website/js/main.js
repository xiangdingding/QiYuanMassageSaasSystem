// ===== 齐源科技官网 交互脚本 =====
(function () {
  'use strict';

  // 年份
  var yearEl = document.getElementById('year');
  if (yearEl) yearEl.textContent = new Date().getFullYear();

  // 顶栏滚动阴影
  var header = document.getElementById('siteHeader');
  function onScroll() {
    if (header) header.classList.toggle('scrolled', window.scrollY > 8);
  }
  window.addEventListener('scroll', onScroll, { passive: true });
  onScroll();

  // 移动端菜单
  var toggle = document.getElementById('navToggle');
  var nav = document.getElementById('primaryNav');
  function closeNav() {
    if (!nav || !toggle) return;
    nav.classList.remove('open');
    toggle.setAttribute('aria-expanded', 'false');
  }
  if (toggle && nav) {
    toggle.addEventListener('click', function () {
      var open = nav.classList.toggle('open');
      toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    });
    nav.addEventListener('click', function (e) {
      if (e.target.tagName === 'A') closeNav();
    });
    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape') closeNav();
    });
  }

  // 滚动揭示动画
  var revealEls = document.querySelectorAll('.reveal');
  if ('IntersectionObserver' in window) {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add('visible');
          io.unobserve(entry.target);
        }
      });
    }, { threshold: 0.12, rootMargin: '0px 0px -40px 0px' });
    revealEls.forEach(function (el) { io.observe(el); });
  } else {
    revealEls.forEach(function (el) { el.classList.add('visible'); });
  }

  // 业务咨询窗口：与齐源按摩 SaaS 子网站调用同一后端接口 POST /api/consultations。
  // 子网站同源走相对 /api（部署处反代到后端）；本站为独立静态站，如与后端不同域，
  // 在页面里设置 window.QIYUAN_API_BASE = 'https://你的后端域名/api' 即可覆盖。
  var API_BASE = (window.QIYUAN_API_BASE || '/api').replace(/\/+$/, '');
  var form = document.getElementById('consultForm');
  if (form) {
    var statusEl = document.getElementById('consultStatus');
    var submitBtn = document.getElementById('consultSubmit');
    var setStatus = function (msg, type) {
      if (!statusEl) return;
      statusEl.textContent = msg || '';
      statusEl.className = 'consult-status' + (type ? ' is-' + type : '');
    };
    form.addEventListener('submit', function (e) {
      e.preventDefault();
      var phone = (form.phone.value || '').trim();
      var content = (form.content.value || '').trim();
      if (!phone) { setStatus('请填写联系电话', 'error'); form.phone.focus(); return; }
      if (phone.length < 5 || phone.length > 32) { setStatus('请输入正确的联系电话', 'error'); form.phone.focus(); return; }
      if (!content) { setStatus('请填写咨询内容', 'error'); form.content.focus(); return; }

      submitBtn.disabled = true;
      submitBtn.textContent = '提交中…';
      setStatus('', '');

      fetch(API_BASE + '/consultations', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          contactName: (form.contactName.value || '').trim() || null,
          phone: phone,
          content: content,
          source: 'website:company'
        })
      }).then(function (res) {
        return res.json().catch(function () { return {}; }).then(function (data) {
          if (!res.ok) throw new Error((data && data.message) || '提交失败');
          return data;
        });
      }).then(function (data) {
        setStatus((data && data.message) || '提交成功，我们会尽快与您联系', 'success');
        form.reset();
      }).catch(function () {
        setStatus('提交失败，请稍后重试或直接电话联系我们', 'error');
      }).finally(function () {
        submitBtn.disabled = false;
        submitBtn.textContent = '提交咨询';
      });
    });
  }
})();
