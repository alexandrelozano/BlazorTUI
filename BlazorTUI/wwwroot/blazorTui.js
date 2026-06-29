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
    "Escape",
    "F2",
    "F4",
    "F10",
    "Home",
    "End",
    "PageUp",
    "PageDown",
    "ContextMenu"
]);

const clipboardKeys = new Set(["a", "c", "x", "v"]);
const editHistoryKeys = new Set(["y", "z"]);

export function attachKeyboardHandling(element, dotNetReference) {
    let pasteHandledByShortcut = false;
    const focusTerminal = () => {
        if (document.activeElement !== element) {
            element.focus({ preventScroll: true });
        }
    };

    element.addEventListener("pointerdown", focusTerminal, { capture: true });

    element.addEventListener("keydown", async event => {
        const shortcut = findShortcut(element, event);
        if (shortcut) {
            event.preventDefault();

            const action = getShortcutAction(shortcut);
            if (shouldHandleShortcutInJavaScript(shortcut) && isShortcutAvailable(action, event, element)) {
                event.stopImmediatePropagation();

                if (event.repeat) {
                    return;
                }

                if (await handleConfiguredShortcut(action, event, element, dotNetReference, value => {
                    pasteHandledByShortcut = value;
                })) {
                    return;
                }
            }
        }

        const acceleratorPressed = event.ctrlKey || event.metaKey;
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

function findShortcut(element, event) {
    let shortcuts = [];
    try {
        shortcuts = JSON.parse(element.dataset.shortcuts || "[]");
    }
    catch {
        shortcuts = [];
    }

    const eventKey = normalizeKey(event.key);
    const eventControl = eventKey === "Control" ? false : event.ctrlKey;
    const eventShift = eventKey === "Shift" ? false : event.shiftKey;
    const eventAlt = eventKey === "Alt" ? false : event.altKey;
    const eventMeta = eventKey === "Meta" ? false : event.metaKey;

    return shortcuts.find(shortcut =>
        normalizeKey(shortcut.Key ?? shortcut.key) === eventKey &&
        Boolean(shortcut.Control ?? shortcut.control) === eventControl &&
        Boolean(shortcut.Shift ?? shortcut.shift) === eventShift &&
        Boolean(shortcut.Alt ?? shortcut.alt) === eventAlt &&
        Boolean(shortcut.Meta ?? shortcut.meta) === eventMeta);
}

function normalizeKey(key) {
    if (key === "Ctrl") {
        return "Control";
    }

    if (key === "Cmd" || key === "Command" || key === "Win" || key === "Windows") {
        return "Meta";
    }

    if (key === "Space" || key === "Spacebar") {
        return " ";
    }

    if (typeof key === "string" && key.length === 1) {
        return key.toUpperCase();
    }

    return key;
}

function shouldHandleShortcutInJavaScript(shortcut) {
    const key = normalizeKey(shortcut.Key ?? shortcut.key);
    return Boolean(shortcut.Control ?? shortcut.control) ||
        Boolean(shortcut.Meta ?? shortcut.meta) ||
        (Boolean(shortcut.Alt ?? shortcut.alt) && key !== "Alt");
}

function getShortcutAction(shortcut) {
    return shortcut.Action ?? shortcut.action;
}

function isShortcutAvailable(action, event, element) {
    switch (action) {
        case "ToggleCommandPalette":
            return element.dataset.commandPaletteEnabled === "true";
        case "SelectNextTab":
        case "SelectPreviousTab":
            return element.dataset.tabNavigationEnabled === "true";
        case "SelectAll":
            return element.dataset.clipboardEnabled === "true";
        case "Copy":
        case "Cut":
            return element.dataset.clipboardEnabled === "true" &&
                element.dataset.clipboardCopyEnabled === "true";
        case "Paste":
            return element.dataset.clipboardEnabled === "true" &&
                element.dataset.clipboardPasteEnabled === "true" &&
                Boolean(navigator.clipboard?.readText);
        case "Undo":
        case "Redo":
            return element.dataset.editHistoryEnabled === "true";
        default:
            return true;
    }
}

async function handleConfiguredShortcut(action, event, element, dotNetReference, setPasteInProgress) {
    try {
        switch (action) {
            case "SelectAll":
                await dotNetReference.invokeMethodAsync("BlazorTUISelectAll");
                return true;
            case "Copy": {
                const text = await dotNetReference.invokeMethodAsync("BlazorTUICopySelection");
                if (text !== null) {
                    await writeClipboardText(text, element);
                }
                return true;
            }
            case "Cut": {
                const text = await dotNetReference.invokeMethodAsync("BlazorTUICopySelection");
                if (text !== null && await writeClipboardText(text, element)) {
                    await dotNetReference.invokeMethodAsync("BlazorTUICutSelection");
                }
                return true;
            }
            case "Paste":
                setPasteInProgress(true);
                await dotNetReference.invokeMethodAsync("BlazorTUIPaste", await navigator.clipboard.readText());
                return true;
            case "Undo":
                await dotNetReference.invokeMethodAsync("BlazorTUIUndo");
                return true;
            case "Redo":
                await dotNetReference.invokeMethodAsync("BlazorTUIRedo");
                return true;
            case "SelectNextTab":
                await dotNetReference.invokeMethodAsync("BlazorTUIMoveTab", false);
                return true;
            case "SelectPreviousTab":
                await dotNetReference.invokeMethodAsync("BlazorTUIMoveTab", true);
                return true;
            case "ToggleCommandPalette":
                await dotNetReference.invokeMethodAsync("BlazorTUIToggleCommandPalette");
                return true;
            default:
                await dotNetReference.invokeMethodAsync("BlazorTUIHandleShortcut", action);
                return true;
        }
    }
    catch {
        // The component may have been disposed, or the browser may deny clipboard access.
        return true;
    }
    finally {
        if (action === "Paste") {
            setPasteInProgress(false);
        }
    }
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
