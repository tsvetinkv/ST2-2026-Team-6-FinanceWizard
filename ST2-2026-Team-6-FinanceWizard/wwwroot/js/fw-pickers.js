// FinanceWizard - form helpers for the add / edit forms of categories, tags and merchants:
// colour picker, icon picker (optional or required), live name preview, and the merchant
// logo preview for an uploaded photo or an image link. Plain JavaScript, no dependencies.
(function () {
    'use strict';

    // ---------- colour helpers (same maths as Colors.TextOn in C#) ----------
    function channel(v) {
        var c = v / 255;
        return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
    }
    function luminance(hex) {
        var r = parseInt(hex.substr(1, 2), 16);
        var g = parseInt(hex.substr(3, 2), 16);
        var b = parseInt(hex.substr(5, 2), 16);
        return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
    }
    function textOn(hex) {
        var bg = luminance(hex);
        var white = 1.05 / (bg + 0.05);
        var dark = (bg + 0.05) / (luminance('#1b2236') + 0.05);
        return white >= dark ? '#ffffff' : '#1b2236';
    }
    function isHex(v) { return /^#[0-9a-fA-F]{6}$/.test(v); }

    // Same automatic hue as Hue.Of in C# (32-bit hash of the lower-cased name)
    function hueOf(text) {
        var hash = 17;
        var s = (text || '').trim().toLowerCase();
        for (var i = 0; i < s.length; i++) {
            hash = (Math.imul(hash, 31) + s.charCodeAt(i)) | 0;
        }
        return Math.abs(hash % 360);
    }

    // ---------- colour picker: ANY colour, square + hex box stay in sync ----------
    var colorInput = document.getElementById('Color');
    var hexInput = document.getElementById('ColorHex');
    var colorPreviews = document.querySelectorAll('[data-color-preview]');

    function applyColor(hex) {
        for (var i = 0; i < colorPreviews.length; i++) {
            var el = colorPreviews[i];
            el.style.setProperty('--c', hex);
            el.style.setProperty('--on', textOn(hex));
            el.classList.add('has-color');
        }
    }

    if (colorInput && hexInput) {
        applyColor(colorInput.value);

        colorInput.addEventListener('input', function () {
            hexInput.value = colorInput.value;
            hexInput.classList.remove('input-validation-error');
            applyColor(colorInput.value);
        });

        hexInput.addEventListener('input', function () {
            var v = hexInput.value.trim();
            if (v && v.charAt(0) !== '#') v = '#' + v;
            if (isHex(v)) {
                colorInput.value = v.toLowerCase();
                hexInput.classList.remove('input-validation-error');
                applyColor(colorInput.value);
            } else {
                hexInput.classList.add('input-validation-error');
            }
        });

        // Leaving the box with a half-typed value puts the real colour back
        hexInput.addEventListener('blur', function () {
            hexInput.value = colorInput.value;
            hexInput.classList.remove('input-validation-error');
        });
    }

    // ---------- logo preview (merchant form): photo or image link ----------
    var logoInput = document.getElementById('Logo');
    var linkInput = document.getElementById('ImageLink');
    var logoPreview = document.querySelector('[data-logo-preview]');
    var logoError = document.getElementById('logoError');
    var MAX_LOGO = 2 * 1024 * 1024;

    var logoMode = false;           // true while a picture (photo or link) is shown
    var objectUrl = null;
    var originalHtml = logoPreview ? logoPreview.innerHTML : '';
    var originalClass = logoPreview ? logoPreview.className : '';

    function fileChosen() {
        return !!(logoInput && logoInput.files && logoInput.files.length > 0);
    }

    // ---------- live name preview ----------
    var nameInput = document.querySelector('[data-name-source]');
    var namePreview = document.querySelector('[data-name-preview]');

    function refreshName() {
        if (!nameInput) return;
        var v = nameInput.value.trim();
        if (namePreview) namePreview.textContent = v || namePreview.getAttribute('data-empty') || '';

        // The letter / hash in the avatar follows the name, as long as no picture is shown
        var initial = document.querySelector('[data-initial-preview]');
        if (initial) initial.textContent = v ? v.charAt(0).toUpperCase() : '?';

        if (logoPreview && logoPreview.hasAttribute('data-auto-hue') && !logoMode) {
            logoPreview.style.setProperty('--h', hueOf(v));
        }
    }

    if (nameInput) {
        nameInput.addEventListener('input', refreshName);
        refreshName();
    }

    function showPicture(src) {
        logoPreview.classList.add('fw-avatar-logo');
        logoPreview.innerHTML = '';
        var img = document.createElement('img');
        img.alt = '';
        img.src = src;
        logoPreview.appendChild(img);
        logoMode = true;
    }

    function showDefaultAvatar() {
        logoPreview.className = originalClass;
        logoPreview.innerHTML = originalHtml;
        logoMode = false;
        refreshName();
    }

    if (logoInput && logoPreview) {
        logoInput.addEventListener('change', function () {
            if (objectUrl) { URL.revokeObjectURL(objectUrl); objectUrl = null; }
            if (logoError) logoError.textContent = '';

            var file = logoInput.files && logoInput.files[0];
            if (!file) {
                showDefaultAvatar();
                return;
            }
            if (!/^image\/(png|jpeg|gif|webp)$/.test(file.type)) {
                if (logoError) logoError.textContent = 'The logo must be a PNG, JPG, GIF or WebP image.';
                logoInput.value = '';
                showDefaultAvatar();
                return;
            }
            if (file.size > MAX_LOGO) {
                if (logoError) logoError.textContent = 'The logo must be 2 MB or smaller.';
                logoInput.value = '';
                showDefaultAvatar();
                return;
            }

            objectUrl = URL.createObjectURL(file);
            showPicture(objectUrl);
        });
    }

    if (linkInput && logoPreview) {
        var linkTimer = null;
        linkInput.addEventListener('input', function () {
            clearTimeout(linkTimer);
            linkTimer = setTimeout(function () {
                var v = linkInput.value.trim();
                if (!/^https?:\/\/\S+$/i.test(v)) {
                    if (!fileChosen()) showDefaultAvatar();
                    return;
                }
                // Only show the picture once the browser has actually loaded it
                var probe = new Image();
                probe.onload = function () {
                    if (linkInput.value.trim() === v && !fileChosen()) showPicture(v);
                };
                probe.onerror = function () {
                    if (linkInput.value.trim() === v && !fileChosen()) showDefaultAvatar();
                };
                probe.src = v;
            }, 400);
        });
    }

    // ---------- icon picker: the whole Bootstrap Icons library, with search ----------
    // data-optional="true": the picker also offers "No icon" (the value is then empty)
    var picker = document.getElementById('iconPicker');
    if (picker) {
        var optional = picker.getAttribute('data-optional') === 'true';
        var hidden = document.getElementById('Icon');
        var grid = document.getElementById('iconGrid');
        var search = document.getElementById('iconSearch');
        var moreBtn = document.getElementById('iconMore');
        var countEl = document.getElementById('iconCount');
        var previewIcon = document.querySelector('[data-icon-preview]');
        var hashPreview = document.querySelector('[data-hash-preview]');
        var iconLabel = document.getElementById('iconLabel');
        var PAGE = 120;

        var allIcons = [];
        var matches = [];
        var shown = 0;

        var select = function (name) {
            hidden.value = name;
            if (previewIcon) {
                previewIcon.className = 'bi bi-' + name + ' fw-bi';
                previewIcon.hidden = !name;
            }
            if (hashPreview) hashPreview.hidden = !!name;
            if (iconLabel) iconLabel.textContent = name || 'none';
            var buttons = grid.querySelectorAll('.fw-pick');
            for (var i = 0; i < buttons.length; i++) {
                buttons[i].setAttribute('aria-pressed', buttons[i].getAttribute('data-icon') === name ? 'true' : 'false');
            }
        };

        var makeButton = function (name, label) {
            var b = document.createElement('button');
            b.type = 'button';
            b.className = 'fw-pick';
            b.setAttribute('data-icon', name);
            b.setAttribute('title', label);
            b.setAttribute('aria-label', label);
            b.setAttribute('aria-pressed', name === hidden.value ? 'true' : 'false');
            return b;
        };

        var appendPage = function () {
            var frag = document.createDocumentFragment();
            var end = Math.min(shown + PAGE, matches.length);
            for (var i = shown; i < end; i++) {
                var name = matches[i];
                var b = makeButton(name, name);
                var ic = document.createElement('i');
                ic.className = 'bi bi-' + name;
                ic.setAttribute('aria-hidden', 'true');
                b.appendChild(ic);
                frag.appendChild(b);
            }
            grid.appendChild(frag);
            shown = end;
            moreBtn.hidden = shown >= matches.length;
            countEl.textContent = matches.length === 0
                ? 'No icons match your search.'
                : 'Showing ' + shown + ' of ' + matches.length + ' icons';
        };

        var applyFilter = function () {
            var terms = search.value.trim().toLowerCase().split(/\s+/).filter(Boolean);
            matches = allIcons.filter(function (n) {
                for (var i = 0; i < terms.length; i++) {
                    if (n.indexOf(terms[i]) === -1) return false;
                }
                return true;
            });
            grid.innerHTML = '';
            if (optional) {
                var none = makeButton('', 'No icon');
                none.classList.add('fw-pick-none');
                none.textContent = 'No icon';
                grid.appendChild(none);
            }
            shown = 0;
            appendPage();
        };

        grid.addEventListener('click', function (e) {
            var btn = e.target.closest ? e.target.closest('.fw-pick') : null;
            if (btn) select(btn.getAttribute('data-icon') || '');
        });
        moreBtn.addEventListener('click', appendPage);
        search.addEventListener('input', applyFilter);
        // Enter in the search box must not submit the form
        search.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') e.preventDefault();
        });

        countEl.textContent = 'Loading icons...';
        fetch(picker.getAttribute('data-icons-url'))
            .then(function (r) { return r.json(); })
            .then(function (data) {
                allIcons = Object.keys(data).sort();
                applyFilter();
            })
            .catch(function () {
                countEl.textContent = 'The icon list could not be loaded. You can still save with the current icon.';
            });
    }
})();
