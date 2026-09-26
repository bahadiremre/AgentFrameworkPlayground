// Shows only the fields and hints that apply to the selected provider type / auth mode.
(function () {
    const form = document.getElementById('provider-form');
    if (!form) return;

    const typeSelect = form.querySelector('[name="Input.ProviderType"]');
    const authRadios = form.querySelectorAll('[name="Input.AuthMode"]');

    function selectedType() {
        // The select posts enum names; its value may be the numeric index, so map via option text order.
        const values = ['AzureAIFoundry', 'OpenAI', 'OpenAICompatible'];
        const v = typeSelect.value;
        return values.includes(v) ? v : values[Number(v)];
    }

    function selectedAuth(type) {
        if (type !== 'AzureAIFoundry') return 'ApiKey';
        const checked = [...authRadios].find(r => r.checked);
        return checked ? checked.value : 'ApiKey';
    }

    function update() {
        const type = selectedType();
        const auth = selectedAuth(type);

        form.querySelectorAll('[data-show-for]').forEach(el => {
            el.hidden = !el.dataset.showFor.split(' ').includes(type);
        });
        form.querySelectorAll('[data-show-for-auth]').forEach(el => {
            el.hidden = el.dataset.showForAuth !== auth;
        });
    }

    typeSelect.addEventListener('change', update);
    authRadios.forEach(r => r.addEventListener('change', update));
    update();
})();
