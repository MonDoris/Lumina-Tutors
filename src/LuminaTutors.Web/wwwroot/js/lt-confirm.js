/**
 * Lumina Tutors — Beautiful Confirm Dialog
 * Replaces native browser confirm() with a polished modal.
 *
 * Usage:
 *  1. Include this file (+ lt-confirm.css) once per page.
 *  2. All onclick="return confirm('...')" buttons are intercepted automatically.
 *  3. For manual use: window.ltConfirm('message', callbackFn)
 */
(function () {
    'use strict';

    // ── Inject HTML ──────────────────────────────────────────────────────────
    const html = `
<div id="lt-confirm-overlay" aria-hidden="true" role="dialog" aria-modal="true" aria-labelledby="lt-cd-title">
  <div id="lt-confirm-dialog">
    <div class="lt-cd__icon-wrap" id="lt-cd-icon-wrap">
      <i class="bi lt-cd__icon" id="lt-cd-icon"></i>
    </div>
    <div class="lt-cd__body">
      <h3 class="lt-cd__title" id="lt-cd-title">Xác nhận</h3>
      <p  class="lt-cd__msg"   id="lt-cd-msg"></p>
    </div>
    <div class="lt-cd__actions">
      <button class="lt-cd__btn lt-cd__btn-cancel" id="lt-cd-cancel" type="button">
        <i class="bi bi-x-lg"></i> Hủy
      </button>
      <button class="lt-cd__btn lt-cd__btn-ok" id="lt-cd-ok" type="button">
        <i class="bi bi-check-lg" id="lt-cd-ok-icon"></i>
        <span id="lt-cd-ok-label">Xác nhận</span>
      </button>
    </div>
  </div>
</div>`;

    // Don't inject twice
    if (document.getElementById('lt-confirm-overlay')) return;
    document.body.insertAdjacentHTML('beforeend', html);

    const overlay  = document.getElementById('lt-confirm-overlay');
    const msgEl    = document.getElementById('lt-cd-msg');
    const iconWrap = document.getElementById('lt-cd-icon-wrap');
    const iconEl   = document.getElementById('lt-cd-icon');
    const okBtn    = document.getElementById('lt-cd-ok');
    const okIcon   = document.getElementById('lt-cd-ok-icon');
    const okLabel  = document.getElementById('lt-cd-ok-label');
    const cancelBtn= document.getElementById('lt-cd-cancel');

    let pendingAction = null;

    // ── Type detection ───────────────────────────────────────────────────────
    const TYPES = {
        danger : {
            iconClass: 'bi-trash3-fill',   wrapClass: 'lt-cd__icon-wrap--danger',
            btnClass : 'lt-cd__btn-ok--danger',  btnLabel: 'Xóa',
            btnIcon  : 'bi-trash3',        title: 'Xác nhận xóa',
            keywords : ['xóa', 'xoá', 'thu hồi', 'vô hiệu']
        },
        warning: {
            iconClass: 'bi-exclamation-triangle-fill', wrapClass: 'lt-cd__icon-wrap--warning',
            btnClass : 'lt-cd__btn-ok--warning', btnLabel: 'Xác nhận',
            btnIcon  : 'bi-check-lg',      title: 'Xác nhận',
            keywords : ['kết thúc', 'đóng phòng', 'đóng phiên', 'đóng đề',
                        'khóa sổ', 'hủy buổi', 'hủy liên kết', 'xếp phòng']
        },
        safe   : {
            iconClass: 'bi-send-fill',     wrapClass: 'lt-cd__icon-wrap--safe',
            btnClass : 'lt-cd__btn-ok--safe', btnLabel: 'Nộp bài',
            btnIcon  : 'bi-send-fill',     title: 'Xác nhận nộp bài',
            keywords : ['nộp bài', 'nộp lại']
        },
        info   : {
            iconClass: 'bi-info-circle-fill', wrapClass: 'lt-cd__icon-wrap--info',
            btnClass : 'lt-cd__btn-ok--info', btnLabel: 'Xác nhận',
            btnIcon  : 'bi-check-lg',      title: 'Xác nhận',
            keywords : ['phát sinh', 'tính điểm', 'tính đtb', 'xếp phòng ngẫu nhiên']
        },
    };

    function detectType(msg) {
        const lower = msg.toLowerCase();
        for (const cfg of Object.values(TYPES))
            if (cfg.keywords.some(k => lower.includes(k)))
                return cfg;
        return TYPES.warning;
    }

    // ── Show / Hide ──────────────────────────────────────────────────────────
    function show(msg, onOk) {
        const cfg = detectType(msg);

        iconWrap.className   = 'lt-cd__icon-wrap ' + cfg.wrapClass;
        iconEl.className     = 'bi lt-cd__icon '   + cfg.iconClass;
        okBtn.className      = 'lt-cd__btn lt-cd__btn-ok ' + cfg.btnClass;
        okIcon.className     = 'bi ' + cfg.btnIcon;
        okLabel.textContent  = cfg.btnLabel;
        document.getElementById('lt-cd-title').textContent = cfg.title;
        msgEl.textContent    = msg;

        pendingAction = onOk;
        overlay.setAttribute('aria-hidden', 'false');
        overlay.classList.add('lt-cd--show');
        requestAnimationFrame(() => okBtn.focus());
    }

    function hide() {
        overlay.classList.remove('lt-cd--show');
        overlay.setAttribute('aria-hidden', 'true');
        pendingAction = null;
    }

    // ── Button events ────────────────────────────────────────────────────────
    okBtn.addEventListener('click', () => { const a = pendingAction; hide(); if (a) a(); });
    cancelBtn.addEventListener('click', hide);
    overlay.addEventListener('click', e => { if (e.target === overlay) hide(); });

    overlay.addEventListener('keydown', e => {
        if (e.key === 'Escape') { hide(); return; }
        if (e.key === 'Enter')  { const a = pendingAction; hide(); if (a) a(); return; }
        if (e.key === 'Tab')    { e.preventDefault(); (document.activeElement === okBtn ? cancelBtn : okBtn).focus(); }
    });

    // ── Global interceptor ───────────────────────────────────────────────────
    const RE_CONFIRM = /\bconfirm\s*\(\s*(['"])([\s\S]*?)\1\s*\)/;

    document.addEventListener('click', function (e) {
        let el = e.target;
        while (el && el !== document.body) {
            const oc = el.getAttribute && el.getAttribute('onclick');
            if (oc) {
                const m = RE_CONFIRM.exec(oc);
                if (m) {
                    if (el.dataset.ltOk === '1') { delete el.dataset.ltOk; return; }
                    e.preventDefault();
                    e.stopImmediatePropagation();
                    const msg  = m[2].replace(/\\n/g, '\n').replace(/\\'/g, "'").replace(/\\"/g, '"');
                    const form = el.closest('form');
                    show(msg, () => {
                        if (form) { form.submit(); }
                        else      { el.dataset.ltOk = '1'; el.click(); }
                    });
                    return;
                }
            }
            el = el.parentElement;
        }
    }, true);

    // ── Public API ───────────────────────────────────────────────────────────
    window.ltConfirm = show;
})();
