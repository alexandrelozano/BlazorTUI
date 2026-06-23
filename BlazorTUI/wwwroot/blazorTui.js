const handledKeys = new Set([
    "Alt",
    "Tab",
    "ArrowUp",
    "ArrowDown",
    "ArrowLeft",
    "ArrowRight",
    "Enter",
    " ",
    "Spacebar",
    "Backspace",
    "Home",
    "End"
]);

export function attachKeyboardHandling(element) {
    element.addEventListener("keydown", event => {
        if (event.ctrlKey || event.metaKey || (event.altKey && event.key !== "Alt")) {
            return;
        }

        if (handledKeys.has(event.key)) {
            event.preventDefault();
        }
    }, { capture: true });
}
