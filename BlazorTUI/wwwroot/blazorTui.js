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
    "Delete",
    "Home",
    "End"
]);

const clipboardKeys = new Set(["a", "c", "x", "v"]);
const editHistoryKeys = new Set(["y", "z"]);

export function attachKeyboardHandling(element, dotNetReference) {
    let pasteHandledByShortcut = false;

    element.addEventListener("keydown", async event => {
        const acceleratorPressed = event.ctrlKey || event.metaKey;
        const shortcutKey = event.key.toLowerCase();
        if (event.ctrlKey && !event.metaKey && !event.altKey && event.key === "Tab" && element.dataset.tabNavigationEnabled === "true") {
            event.preventDefault();
            try {
                await dotNetReference.invokeMethodAsync("BlazorTUIMoveTab", event.shiftKey);
            }
            catch {
                // The component may have been disposed while the browser event was pending.
            }
            return;
        }

        if (acceleratorPressed && !event.altKey && editHistoryKeys.has(shortcutKey)) {
            if (element.dataset.editHistoryEnabled !== "true") {
                return;
            }

            event.preventDefault();
            const redo = shortcutKey === "y" || (shortcutKey === "z" && event.shiftKey);
            const method = redo ? "BlazorTUIRedo" : "BlazorTUIUndo";
            try {
                await dotNetReference.invokeMethodAsync(method);
            }
            catch {
                // The component may have been disposed while the browser event was pending.
            }
            return;
        }

        const clipboardKey = shortcutKey;
        if (acceleratorPressed && !event.altKey && clipboardKeys.has(clipboardKey)) {
            if (element.dataset.clipboardEnabled !== "true") {
                return;
            }

            if ((clipboardKey === "c" || clipboardKey === "x") && element.dataset.clipboardCopyEnabled !== "true") {
                return;
            }

            if (clipboardKey === "v" && element.dataset.clipboardPasteEnabled !== "true") {
                return;
            }

            if (clipboardKey === "v" && !navigator.clipboard?.readText) {
                return;
            }

            event.preventDefault();
            await handleClipboardShortcut(clipboardKey, element, dotNetReference, value => {
                pasteHandledByShortcut = value;
            });
            return;
        }

        if (event.ctrlKey || event.metaKey || (event.altKey && event.key !== "Alt")) {
            return;
        }

        if (handledKeys.has(event.key)) {
            event.preventDefault();
        }
    }, { capture: true });

    element.addEventListener("paste", event => {
        if (element.dataset.clipboardEnabled !== "true" || element.dataset.clipboardPasteEnabled !== "true") {
            return;
        }

        if (pasteHandledByShortcut) {
            event.preventDefault();
            return;
        }

        const text = event.clipboardData?.getData("text/plain");
        if (typeof text !== "string") {
            return;
        }

        event.preventDefault();
        void dotNetReference.invokeMethodAsync("BlazorTUIPaste", text).catch(() => { });
    });
}

async function handleClipboardShortcut(key, element, dotNetReference, setPasteInProgress) {
    try {
        switch (key) {
            case "a":
                await dotNetReference.invokeMethodAsync("BlazorTUISelectAll");
                break;
            case "c": {
                const text = await dotNetReference.invokeMethodAsync("BlazorTUICopySelection");
                if (text !== null) {
                    await writeClipboardText(text, element);
                }
                break;
            }
            case "x": {
                const text = await dotNetReference.invokeMethodAsync("BlazorTUICopySelection");
                if (text !== null && await writeClipboardText(text, element)) {
                    await dotNetReference.invokeMethodAsync("BlazorTUICutSelection");
                }
                break;
            }
            case "v": {
                setPasteInProgress(true);
                const text = await navigator.clipboard.readText();
                await dotNetReference.invokeMethodAsync("BlazorTUIPaste", text);
                break;
            }
        }
    }
    catch {
        // Clipboard permissions and availability are controlled by the browser.
    }
    finally {
        setPasteInProgress(false);
    }
}

async function writeClipboardText(text, element) {
    if (navigator.clipboard?.writeText) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        }
        catch {
            // Fall through to the legacy browser copy command.
        }
    }

    const textArea = document.createElement("textarea");
    textArea.value = text;
    textArea.setAttribute("readonly", "");
    textArea.style.position = "fixed";
    textArea.style.opacity = "0";
    document.body.appendChild(textArea);
    textArea.select();

    let copied = false;
    try {
        copied = document.execCommand("copy");
    }
    finally {
        textArea.remove();
        element.focus({ preventScroll: true });
    }

    return copied;
}
