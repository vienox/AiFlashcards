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

    return { init, flip, reset };
})();
