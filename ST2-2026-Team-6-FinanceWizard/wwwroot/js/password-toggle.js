(function () {
    var EYE = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>';
    var EYE_OFF = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><path d="M1 1l22 22"/></svg>';

    function enhance(input) {
        if (input.dataset.pwEnhanced) return;
        input.dataset.pwEnhanced = '1';

        var parent = input.parentElement;
        var host;
        if (parent.classList.contains('fw-pw') || parent.classList.contains('form-floating')) {
            host = parent;                       // reuse the existing container
            host.classList.add('fw-pw');
        } else {
            host = document.createElement('div'); // wrap the input
            host.className = 'fw-pw';
            parent.insertBefore(host, input);
            host.appendChild(input);
        }
        input.classList.add('fw-pw-input');

        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'fw-pw-toggle';
        btn.setAttribute('aria-label', 'Show password');
        btn.setAttribute('aria-pressed', 'false');
        btn.innerHTML = EYE;
        btn.addEventListener('click', function () {
            var show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            btn.innerHTML = show ? EYE_OFF : EYE;
            btn.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
            btn.setAttribute('aria-pressed', show ? 'true' : 'false');
        });
        host.appendChild(btn);
    }

    document.querySelectorAll('input[type="password"]').forEach(enhance);
})();