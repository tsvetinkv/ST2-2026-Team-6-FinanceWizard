// FinanceWizard - AI category suggestion on the transaction form.
// While the user types a description (or picks a merchant) the server is asked for a category;
// the suggested category is selected and highlighted, unless the user already chose one themselves.
(function () {
    'use strict';

    var hint = document.getElementById('aiCategoryHint');
    var description = document.getElementById('Description');
    var category = document.getElementById('CategoryId');
    var merchant = document.getElementById('MerchantId');
    if (!hint || !description || !category) return;

    var url = hint.getAttribute('data-url');
    var createUrl = hint.getAttribute('data-create-url');
    var DEBOUNCE_MS = 600;
    var MIN_CONFIDENCE = 0.5;

    var userChose = category.value !== '';   // editing a transaction that already has a category
    var timer = null;
    var controller = null;
    var lastKey = null;
    var dismissedKey = null;                 // the user pressed "Undo" for this text

    category.addEventListener('change', function () {
        // Fired only by the user: programmatic changes below do not raise this event
        userChose = category.value !== '';
        if (userChose) { hide(); }
    });

    description.addEventListener('input', function () {
        window.clearTimeout(timer);
        timer = window.setTimeout(request, DEBOUNCE_MS);
    });
    description.addEventListener('blur', function () {
        window.clearTimeout(timer);
        request();
    });
    if (merchant) {
        merchant.addEventListener('change', function () {
            window.clearTimeout(timer);
            request();
        });
    }

    function currentKey() {
        return description.value.trim() + '|' + (merchant ? merchant.value : '');
    }

    function request() {
        var text = description.value.trim();
        var merchantId = merchant ? merchant.value : '';
        if (text.length < 2 && !merchantId) { hide(); lastKey = null; return; }

        var key = currentKey();
        if (key === lastKey) return;
        lastKey = key;

        if (controller) controller.abort();
        controller = typeof AbortController !== 'undefined' ? new AbortController() : null;

        var query = '?description=' + encodeURIComponent(text) + (merchantId ? '&merchantId=' + encodeURIComponent(merchantId) : '');
        fetch(url + query, {
            headers: { 'Accept': 'application/json' },
            credentials: 'same-origin',
            signal: controller ? controller.signal : undefined
        })
            .then(function (response) { return response.ok ? response.json() : null; })
            .then(function (suggestion) {
                if (suggestion && key === currentKey()) show(suggestion, key);
            })
            .catch(function () { /* aborted or offline: the suggestion is only a convenience */ });
    }

    // ---------- UI ----------
    function hide() {
        hint.hidden = true;
        hint.textContent = '';
    }

    function badge() {
        var b = document.createElement('span');
        b.className = 'fw-ai-badge';
        b.textContent = 'AI';
        return b;
    }

    function addText(parts) {
        var span = document.createElement('span');
        parts.forEach(function (p) {
            if (typeof p === 'string') {
                span.appendChild(document.createTextNode(p));
            } else {
                var strong = document.createElement('strong');
                strong.textContent = p.strong;
                span.appendChild(strong);
            }
        });
        hint.appendChild(span);
    }

    function addButton(label, onClick) {
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'fw-ai-action';
        btn.textContent = label;
        btn.addEventListener('click', onClick);
        hint.appendChild(btn);
    }

    function flash() {
        category.classList.remove('fw-ai-flash');
        void category.offsetWidth;           // restart the animation
        category.classList.add('fw-ai-flash');
        window.setTimeout(function () { category.classList.remove('fw-ai-flash'); }, 2000);
    }

    function select(id) {
        category.value = String(id);
        flash();
    }

    function show(s, key) {
        hint.textContent = '';
        var hasMatch = s.categoryId && s.confidence >= MIN_CONFIDENCE;

        if (hasMatch) {
            var id = String(s.categoryId);

            if (!userChose && key !== dismissedKey) {
                // Nothing chosen yet: fill it in and let the user undo
                select(id);
                hint.appendChild(badge());
                addText(['Suggested category: ', { strong: s.categoryName }, s.reason ? ' – ' + s.reason : '']);
                addButton('Undo', function () {
                    category.value = '';
                    dismissedKey = key;
                    hide();
                });
            } else if (category.value === id) {
                hint.appendChild(badge());
                addText([{ strong: s.categoryName }, ' matches the suggestion.']);
            } else {
                // The user picked something else: only offer the suggestion
                hint.appendChild(badge());
                addText(['Suggestion: ', { strong: s.categoryName }]);
                addButton('Use', function () {
                    select(id);
                    userChose = true;
                    hide();
                });
            }
        } else if (s.suggestedConcept) {
            hint.appendChild(badge());
            addText(['Looks like ', { strong: s.suggestedConcept }, ' – you have no matching category yet. ']);
            if (createUrl) {
                var link = document.createElement('a');
                link.href = createUrl;
                link.textContent = 'Create one';
                hint.appendChild(link);
            }
        } else {
            hide();
            return;
        }

        hint.hidden = false;
    }

    // A description may already be filled in (validation error, edit): ask once on load
    if (description.value.trim().length >= 2 && !userChose) request();
})();
