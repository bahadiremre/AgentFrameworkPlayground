// Shows only the fields and hints that apply to the selected provider type / auth mode.
// All per-type rules come from data-* attributes on the <option> elements, rendered from
// the server-side provider definitions, so this script never needs to change for a new type.
(function () {
    const form = document.getElementById('provider-form');
    if (!form) return;

    const typeSelect = form.querySelector('[name="Input.ProviderType"]');
    const authRadios = [...form.querySelectorAll('[name="Input.AuthMode"]')];
    const authGroup = form.querySelector('[data-auth-group]');

    const words = value => (value || '').split(' ').filter(Boolean);

    function update() {
        const option = typeSelect.selectedOptions[0];
        if (!option) return;

        const type = option.value;
        const authModes = words(option.dataset.authModes);
        const keyRequiredModes = words(option.dataset.keyRequiredModes);

        // Fall back to the type's default mode if the checked one isn't supported.
        let auth = authRadios.find(r => r.checked)?.value;
        if (!authModes.includes(auth)) {
            auth = authModes[0];
            authRadios.forEach(r => { r.checked = r.value === auth; });
        }

        authGroup.hidden = authModes.length < 2;
        form.querySelectorAll('[data-auth-mode]').forEach(el => {
            el.hidden = !authModes.includes(el.dataset.authMode);
        });

        form.querySelectorAll('[data-show-for]').forEach(el => {
            el.hidden = el.dataset.showFor !== type;
        });
        form.querySelectorAll('[data-show-for-auth]').forEach(el => {
            el.hidden = el.dataset.showForAuth !== auth;
        });

        form.querySelector('[data-optional-endpoint]').hidden = option.dataset.endpoint !== 'Optional';
        form.querySelector('[data-optional-key]').hidden = keyRequiredModes.includes(auth);
    }

    typeSelect.addEventListener('change', update);
    authRadios.forEach(r => r.addEventListener('change', update));
    update();
})();
