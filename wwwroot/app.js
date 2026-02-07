window.flashcardsTraining = (() => {
    const initialized = new WeakSet();

    function resolveEl(selectorOrEl) {
        if (!selectorOrEl) return null;
        if (typeof selectorOrEl === "string") {
            return document.querySelector(selectorOrEl);
        }
        return selectorOrEl;
    }

    function init(selectorOrEl) {
        const el = resolveEl(selectorOrEl);
        if (!el || initialized.has(el)) return;

        el.addEventListener("click", () => {
            el.classList.toggle("is-flipped");
        });
        resize(el);
        initialized.add(el);
    }

    function flip(selectorOrEl) {
        const el = resolveEl(selectorOrEl);
        if (!el) return;
        el.classList.toggle("is-flipped");
    }

    function reset(selectorOrEl) {
        const el = resolveEl(selectorOrEl);
        if (!el) return;
        el.classList.remove("is-flipped");
    }

    function resize(selectorOrEl) {
        const el = resolveEl(selectorOrEl);
        if (!el) return;
        const targets = el.querySelectorAll("[data-autosize='true']");
        targets.forEach((node) => {
            const text = (node.textContent || "").trim();
            const length = text.length;
            let size = 1.4;
            if (length > 240) size = 0.9;
            else if (length > 180) size = 1.0;
            else if (length > 130) size = 1.1;
            else if (length > 90) size = 1.2;
            else size = 1.45;
            node.style.fontSize = `${size}rem`;
        });
    }

    return { init, flip, reset, resize };
})();

window.flashcardsAuth = (() => {
    async function postJson(url, payload) {
        const response = await fetch(url, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            credentials: "same-origin",
            body: payload ? JSON.stringify(payload) : "{}"
        });

        let data = null;
        try {
            data = await response.json();
        } catch {
            data = null;
        }

        return {
            ok: response.ok,
            status: response.status,
            error: data && data.error ? data.error : null,
            returnUrl: data && data.returnUrl ? data.returnUrl : null
        };
    }

    return { postJson };
})();
