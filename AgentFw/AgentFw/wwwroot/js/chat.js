// Floating chatbot panel (bottom-right). Agent backend is not wired yet;
// replace sendToAgent with a call to the chat endpoint once it exists.
(function () {
    const toggle = document.getElementById('chat-toggle');
    const panel = document.getElementById('chat-panel');
    const closeBtn = document.getElementById('chat-close');
    const form = document.getElementById('chat-form');
    const input = document.getElementById('chat-input');
    const messages = document.getElementById('chat-messages');

    if (!toggle || !panel) return;

    function setOpen(open) {
        panel.hidden = !open;
        toggle.setAttribute('aria-expanded', String(open));
        toggle.setAttribute('aria-label', open ? 'Sohbeti kapat' : 'Sohbeti aç');
        if (open) input.focus();
    }

    function addMessage(text, from) {
        const div = document.createElement('div');
        div.className = 'chat-msg chat-msg-' + from;
        div.textContent = text; // textContent: never inject user text as HTML
        messages.appendChild(div);
        messages.scrollTop = messages.scrollHeight;
        return div;
    }

    async function sendToAgent(text) {
        await new Promise(r => setTimeout(r, 400));
        return 'Agent henüz bağlı değil. Foundry bağlantısı eklenince burada gerçek yanıtlar görünecek.';
    }

    toggle.addEventListener('click', () => setOpen(panel.hidden));
    closeBtn.addEventListener('click', () => setOpen(false));
    document.addEventListener('keydown', e => {
        if (e.key === 'Escape' && !panel.hidden) setOpen(false);
    });

    form.addEventListener('submit', async e => {
        e.preventDefault();
        const text = input.value.trim();
        if (!text) return;

        addMessage(text, 'user');
        input.value = '';
        input.disabled = true;
        const pending = addMessage('...', 'bot');

        try {
            pending.textContent = await sendToAgent(text);
        } catch {
            pending.textContent = 'Bir hata oluştu, tekrar dene.';
        } finally {
            input.disabled = false;
            input.focus();
        }
    });
})();
