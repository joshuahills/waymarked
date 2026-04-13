// ── Auth module ──────────────────────────────────────────────────────
(function () {
    // ── DOM refs ──────────────────────────────────────────────────────
    const signInBtn      = document.getElementById('signInBtn');
    const userInfo       = document.getElementById('userInfo');
    const userEmailEl    = document.getElementById('userEmail');
    const signOutBtn     = document.getElementById('signOutBtn');

    const modal          = document.getElementById('authModal');
    const modalOverlay   = modal.querySelector('.auth-modal-overlay');
    const modalClose     = document.getElementById('authModalClose');
    const modalTitle     = document.getElementById('authModalTitle');

    const loginForm      = document.getElementById('loginForm');
    const loginError     = document.getElementById('loginError');
    const registerForm   = document.getElementById('registerForm');
    const registerError  = document.getElementById('registerError');
    const pwRequirements = document.getElementById('pwRequirements');
    const registerPwInput  = document.getElementById('registerPassword');
    const registerCfmInput = document.getElementById('registerConfirm');
    const registerSubmit   = registerForm.querySelector('button[type="submit"]');

    const switchToReg    = document.getElementById('switchToRegister');
    const switchToLog    = document.getElementById('switchToLogin');

    // ── Modal ─────────────────────────────────────────────────────────

    function openModal(panel) {
        modal.removeAttribute('hidden');
        document.body.style.overflow = 'hidden';
        if (panel === 'register') showRegister();
        else showLogin();
        const firstInput = modal.querySelector('.auth-form:not([hidden]) .auth-input');
        if (firstInput) firstInput.focus();
    }

    function closeModal() {
        modal.setAttribute('hidden', '');
        document.body.style.overflow = '';
        loginForm.reset();
        registerForm.reset();
        clearError(loginError);
        clearError(registerError);
        resetPasswordChecklist();
    }

    function showLogin() {
        loginForm.removeAttribute('hidden');
        registerForm.setAttribute('hidden', '');
        modalTitle.textContent = 'Sign in';
    }

    function showRegister() {
        registerForm.removeAttribute('hidden');
        loginForm.setAttribute('hidden', '');
        modalTitle.textContent = 'Create account';
    }

    // ── Error helpers ─────────────────────────────────────────────────

    function showError(el, msg) {
        el.textContent = msg;
        el.classList.add('visible');
    }

    function clearError(el) {
        el.textContent = '';
        el.classList.remove('visible');
    }

    // ── Nav state ─────────────────────────────────────────────────────

    function setLoggedIn(email) {
        const truncated = email.length > 24 ? email.slice(0, 24) + '…' : email;
        userEmailEl.textContent = truncated;
        userInfo.removeAttribute('hidden');
        signInBtn.setAttribute('hidden', '');
    }

    function setLoggedOut() {
        userInfo.setAttribute('hidden', '');
        signInBtn.removeAttribute('hidden');
    }

    // ── Auth state check on load ──────────────────────────────────────

    async function checkAuth() {
        try {
            const res = await fetch('/api/auth/me', { credentials: 'same-origin' });
            if (res.ok) {
                const data = await res.json();
                setLoggedIn(data.email);
            } else {
                setLoggedOut();
            }
        } catch {
            setLoggedOut();
        }
    }

    // ── Login ─────────────────────────────────────────────────────────

    loginForm.addEventListener('submit', async e => {
        e.preventDefault();
        clearError(loginError);

        const email    = document.getElementById('loginEmail').value.trim();
        const password = document.getElementById('loginPassword').value;

        if (!email || !password) {
            showError(loginError, 'Please enter your email and password');
            return;
        }

        const submitBtn = loginForm.querySelector('button[type="submit"]');
        submitBtn.disabled    = true;
        submitBtn.textContent = 'Signing in…';

        try {
            const res = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify({ email, password })
            });

            if (res.ok) {
                setLoggedIn(email);
                closeModal();
            } else {
                const body = await res.json().catch(() => ({}));
                showError(loginError, body.message || 'Invalid email or password');
            }
        } catch {
            showError(loginError, 'Something went wrong. Please try again.');
        } finally {
            submitBtn.disabled    = false;
            submitBtn.textContent = 'Sign in';
        }
    });

    // ── Register ──────────────────────────────────────────────────────

    // Password requirements (ASP.NET Core Identity defaults)
    const pwRules = {
        length:    pw => pw.length >= 6,
        uppercase: pw => /[A-Z]/.test(pw),
        lowercase: pw => /[a-z]/.test(pw),
        number:    pw => /[0-9]/.test(pw),
        special:   pw => /[^a-zA-Z0-9]/.test(pw),
    };

    function evaluatePassword() {
        const pw      = registerPwInput.value;
        const confirm = registerCfmInput.value;
        let allMet = true;

        for (const [key, test] of Object.entries(pwRules)) {
            const met = test(pw);
            if (!met) allMet = false;
            const item = pwRequirements.querySelector(`[data-req="${key}"]`);
            item.classList.toggle('met', met);
            item.classList.toggle('unmet', !met);
        }

        const confirmMatch = pw.length > 0 && pw === confirm;
        registerSubmit.disabled = !(allMet && confirmMatch);
    }

    function resetPasswordChecklist() {
        for (const item of pwRequirements.querySelectorAll('.pw-req')) {
            item.classList.remove('met', 'unmet');
        }
        registerSubmit.disabled = true;
    }

    registerPwInput.addEventListener('input', evaluatePassword);
    registerCfmInput.addEventListener('input', evaluatePassword);

    // Start disabled — nothing typed yet
    registerSubmit.disabled = true;

    registerForm.addEventListener('submit', async e => {
        e.preventDefault();
        clearError(registerError);

        const email    = document.getElementById('registerEmail').value.trim();
        const password = registerPwInput.value;

        if (!email || !password) {
            showError(registerError, 'Please fill in all fields');
            return;
        }

        const submitBtn = registerForm.querySelector('button[type="submit"]');
        submitBtn.disabled    = true;
        submitBtn.textContent = 'Creating account…';

        try {
            const res = await fetch('/api/auth/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify({ email, password })
            });

            if (res.ok) {
                setLoggedIn(email);
                closeModal();
            } else {
                const body = await res.json().catch(() => ({}));
                const msg = Array.isArray(body.errors) && body.errors.length
                    ? body.errors.join(' ')
                    : (body.message || 'Could not create account. Try a different email.');
                showError(registerError, msg);
            }
        } catch {
            showError(registerError, 'Something went wrong. Please try again.');
        } finally {
            // Re-evaluate so button state reflects current field values
            evaluatePassword();
            submitBtn.textContent = 'Create account';
        }
    });

    // ── Sign out ──────────────────────────────────────────────────────

    signOutBtn.addEventListener('click', async () => {
        signOutBtn.disabled = true;
        try {
            await fetch('/api/auth/logout', {
                method: 'POST',
                credentials: 'same-origin'
            });
        } catch { /* best-effort — clear UI regardless */ }
        setLoggedOut();
        signOutBtn.disabled = false;
    });

    // ── Event wiring ──────────────────────────────────────────────────

    signInBtn.addEventListener('click', () => openModal('login'));
    modalClose.addEventListener('click', closeModal);
    modalOverlay.addEventListener('click', closeModal);

    switchToReg.addEventListener('click', e => { e.preventDefault(); showRegister(); });
    switchToLog.addEventListener('click', e => { e.preventDefault(); showLogin(); });

    document.addEventListener('keydown', e => {
        if (e.key === 'Escape' && !modal.hasAttribute('hidden')) closeModal();
    });

    // ── Init ──────────────────────────────────────────────────────────
    checkAuth();
})();
