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

    const forgotPasswordForm = document.getElementById('forgotPasswordForm');
    const forgotError        = document.getElementById('forgotError');
    const forgotSuccess      = document.getElementById('forgotSuccess');
    const forgotSubmit       = document.getElementById('forgotSubmit');

    const resetPasswordForm  = document.getElementById('resetPasswordForm');
    const resetError         = document.getElementById('resetError');
    const resetSubmit        = document.getElementById('resetSubmit');
    const resetPwInput       = document.getElementById('resetPassword');
    const resetCfmInput      = document.getElementById('resetConfirm');
    const resetPwRequirements = document.getElementById('resetPwRequirements');

    const switchToReg    = document.getElementById('switchToRegister');
    const switchToLog    = document.getElementById('switchToLogin');
    const switchToForgot = document.getElementById('switchToForgotPassword');
    const switchToLogFromForgot = document.getElementById('switchToLoginFromForgot');

    // Token and email captured from URL params for the reset-password flow
    let _resetToken = null;
    let _resetEmail = null;

    // ── Modal ─────────────────────────────────────────────────────────

    const allForms = [loginForm, registerForm, forgotPasswordForm, resetPasswordForm];

    function hideAllForms() {
        allForms.forEach(f => f.setAttribute('hidden', ''));
    }

    function openModal(panel) {
        modal.removeAttribute('hidden');
        document.body.style.overflow = 'hidden';
        if (panel === 'register') showRegister();
        else if (panel === 'forgot-password') showForgotPassword();
        else if (panel === 'reset-password') showResetPassword();
        else showLogin();
        const firstInput = modal.querySelector('.auth-form:not([hidden]) .auth-input');
        if (firstInput) firstInput.focus();
    }

    function closeModal() {
        modal.setAttribute('hidden', '');
        document.body.style.overflow = '';
        loginForm.reset();
        registerForm.reset();
        forgotPasswordForm.reset();
        resetPasswordForm.reset();
        clearError(loginError);
        clearError(registerError);
        clearError(forgotError);
        clearError(resetError);
        forgotSuccess.setAttribute('hidden', '');
        forgotSubmit.disabled = false;
        forgotSubmit.textContent = 'Send reset link';
        resetPasswordChecklist(pwRequirements);
        resetPasswordChecklist(resetPwRequirements);
    }

    function showLogin() {
        hideAllForms();
        loginForm.removeAttribute('hidden');
        modalTitle.textContent = 'Sign in';
    }

    function showRegister() {
        hideAllForms();
        registerForm.removeAttribute('hidden');
        modalTitle.textContent = 'Create account';
    }

    function showForgotPassword() {
        hideAllForms();
        forgotPasswordForm.removeAttribute('hidden');
        modalTitle.textContent = 'Reset password';
    }

    function showResetPassword() {
        hideAllForms();
        resetPasswordForm.removeAttribute('hidden');
        modalTitle.textContent = 'Choose a new password';
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
        userEmailEl.textContent = '';
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

    function evaluatePasswordChecklist(pwInput, cfmInput, requirementsEl, submitBtn) {
        const pw      = pwInput.value;
        const confirm = cfmInput.value;
        let allMet = true;

        for (const [key, test] of Object.entries(pwRules)) {
            const met = test(pw);
            if (!met) allMet = false;
            const item = requirementsEl.querySelector(`[data-req="${key}"]`);
            item.classList.toggle('met', met);
            item.classList.toggle('unmet', !met);
        }

        const confirmMatch = pw.length > 0 && pw === confirm;
        submitBtn.disabled = !(allMet && confirmMatch);
    }

    function resetPasswordChecklist(requirementsEl) {
        for (const item of requirementsEl.querySelectorAll('.pw-req')) {
            item.classList.remove('met', 'unmet');
        }
    }

    registerPwInput.addEventListener('input',  () => evaluatePasswordChecklist(registerPwInput, registerCfmInput, pwRequirements, registerSubmit));
    registerCfmInput.addEventListener('input', () => evaluatePasswordChecklist(registerPwInput, registerCfmInput, pwRequirements, registerSubmit));

    resetPwInput.addEventListener('input',  () => evaluatePasswordChecklist(resetPwInput, resetCfmInput, resetPwRequirements, resetSubmit));
    resetCfmInput.addEventListener('input', () => evaluatePasswordChecklist(resetPwInput, resetCfmInput, resetPwRequirements, resetSubmit));

    // Start disabled — nothing typed yet
    registerSubmit.disabled = true;
    resetSubmit.disabled = true;

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
            evaluatePasswordChecklist(registerPwInput, registerCfmInput, pwRequirements, registerSubmit);
            submitBtn.textContent = 'Create account';
        }
    });

    // ── Forgot password ───────────────────────────────────────────────

    forgotPasswordForm.addEventListener('submit', async e => {
        e.preventDefault();
        clearError(forgotError);
        forgotSuccess.setAttribute('hidden', '');

        const email = document.getElementById('forgotEmail').value.trim();
        if (!email) {
            showError(forgotError, 'Please enter your email address');
            return;
        }

        forgotSubmit.disabled    = true;
        forgotSubmit.textContent = 'Sending…';

        try {
            await fetch('/api/auth/forgot-password', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email })
            });
            // Always show success regardless of whether the email exists
            forgotSuccess.removeAttribute('hidden');
            forgotSubmit.disabled = true;
        } catch {
            showError(forgotError, 'Something went wrong. Please try again.');
            forgotSubmit.disabled    = false;
            forgotSubmit.textContent = 'Send reset link';
        }
    });

    // ── Reset password ────────────────────────────────────────────────

    resetPasswordForm.addEventListener('submit', async e => {
        e.preventDefault();
        clearError(resetError);

        const newPassword = resetPwInput.value;
        if (!newPassword || !_resetToken || !_resetEmail) {
            showError(resetError, 'Invalid reset link. Please request a new one.');
            return;
        }

        resetSubmit.disabled    = true;
        resetSubmit.textContent = 'Resetting…';

        try {
            const res = await fetch('/api/auth/reset-password', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email: _resetEmail, token: _resetToken, newPassword })
            });

            if (res.ok) {
                // Show login form with a success hint
                showLogin();
                showError(loginError, '');
                // Pre-fill the email and show a friendly message
                document.getElementById('loginEmail').value = _resetEmail;
                const hint = document.createElement('p');
                hint.className = 'auth-success';
                hint.style.marginBottom = '0.75rem';
                hint.textContent = 'Password reset successfully. Please sign in with your new password.';
                loginForm.prepend(hint);
                _resetToken = null;
                _resetEmail = null;
            } else {
                const body = await res.json().catch(() => ({}));
                const msg = Array.isArray(body.errors) && body.errors.length
                    ? body.errors.join(' ')
                    : 'Password reset failed. The link may have expired.';
                showError(resetError, msg);
                resetSubmit.disabled    = false;
                resetSubmit.textContent = 'Reset password';
            }
        } catch {
            showError(resetError, 'Something went wrong. Please try again.');
            resetSubmit.disabled    = false;
            resetSubmit.textContent = 'Reset password';
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
    switchToForgot.addEventListener('click', e => { e.preventDefault(); showForgotPassword(); });
    switchToLogFromForgot.addEventListener('click', e => { e.preventDefault(); showLogin(); });

    document.addEventListener('keydown', e => {
        if (e.key === 'Escape' && !modal.hasAttribute('hidden')) closeModal();
    });

    // ── Password reset via URL params ─────────────────────────────────

    function initPasswordReset() {
        const params = new URLSearchParams(location.search);
        const token  = params.get('resetToken');
        const email  = params.get('email');
        if (token && email) {
            _resetToken = token;
            _resetEmail = email;
            // Clean the URL so the token isn't left in browser history
            history.replaceState(null, '', '/');
            openModal('reset-password');
        }
    }

    // ── Init ──────────────────────────────────────────────────────────
    checkAuth();
    initPasswordReset();
})();
