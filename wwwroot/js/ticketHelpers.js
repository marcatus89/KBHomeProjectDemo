window.ticketHelpers = window.ticketHelpers || (function () {
    const th = {};

    th.scrollToComment = function (elemId) {
        try {
            var el = document.getElementById(elemId);
            if (!el) return;
            el.scrollIntoView({ behavior: 'smooth', block: 'center' });
            el.classList.add('highlight-comment');
            setTimeout(function () {
                el.classList.remove('highlight-comment');
            }, 3000);
        } catch (e) {
            console.error(e);
        }
    };

    // state holders
    th._dotNetRef = null;
    th._keydownHandler = null;
    th._imgElement = null;
    th._imgState = null;
    th._imgHandlers = {};

    th.registerPreviewHandlers = function (dotNetRef) {
        try {
            th.unregisterPreviewHandlers(); // cleanup if any
            th._dotNetRef = dotNetRef;

            th._keydownHandler = function (e) {
                // left / right / esc
                if (e.key === 'Escape') {
                    if (th._dotNetRef) th._dotNetRef.invokeMethodAsync('ClosePreviewFromJs');
                } else if (e.key === 'ArrowRight') {
                    if (th._dotNetRef) th._dotNetRef.invokeMethodAsync('PreviewNext');
                } else if (e.key === 'ArrowLeft') {
                    if (th._dotNetRef) th._dotNetRef.invokeMethodAsync('PreviewPrev');
                }
            };
            document.addEventListener('keydown', th._keydownHandler);
            // ensure image transform cleaned
            th.resetImageTransform();
        } catch (e) {
            console.error('registerPreviewHandlers', e);
        }
    };

    th.unregisterPreviewHandlers = function () {
        try {
            if (th._keydownHandler) {
                document.removeEventListener('keydown', th._keydownHandler);
                th._keydownHandler = null;
            }
            th.resetImageTransform();
            th._dotNetRef = null;
        } catch (e) {
            console.error('unregisterPreviewHandlers', e);
        }
    };

    th.resetImageTransform = function () {
        // remove existing image handlers
        try {
            if (th._imgElement && th._imgHandlers.wheel) {
                th._imgElement.removeEventListener('wheel', th._imgHandlers.wheel);
                th._imgElement.removeEventListener('mousedown', th._imgHandlers.mousedown);
                th._imgElement.removeEventListener('dblclick', th._imgHandlers.dblclick);
                document.removeEventListener('mousemove', th._imgHandlers.mousemove);
                document.removeEventListener('mouseup', th._imgHandlers.mouseup);
            }
        } catch (e) { /* ignore */ }

        th._imgElement = document.querySelector('.preview-body img');
        th._imgState = { scale: 1, translateX: 0, translateY: 0, isPanning: false, startX: 0, startY: 0 };

        if (!th._imgElement) return;

        th._imgElement.style.transform = 'translate(0px,0px) scale(1)';
        th._imgElement.style.cursor = 'grab';
        th._imgElement.style.userSelect = 'none';

        th._imgHandlers.wheel = function (ev) {
            ev.preventDefault();
            var delta = ev.deltaY > 0 ? -0.12 : 0.12;
            th._imgState.scale = Math.max(0.2, Math.min(5, th._imgState.scale + delta));
            th._applyTransform();
        };
        th._imgElement.addEventListener('wheel', th._imgHandlers.wheel, { passive: false });

        th._imgHandlers.mousedown = function (ev) {
            ev.preventDefault();
            th._imgState.isPanning = true;
            th._imgState.startX = ev.clientX;
            th._imgState.startY = ev.clientY;
            th._imgElement.style.cursor = 'grabbing';
        };
        th._imgElement.addEventListener('mousedown', th._imgHandlers.mousedown);

        th._imgHandlers.mousemove = function (ev) {
            if (!th._imgState.isPanning) return;
            var dx = ev.clientX - th._imgState.startX;
            var dy = ev.clientY - th._imgState.startY;
            th._imgState.startX = ev.clientX;
            th._imgState.startY = ev.clientY;
            th._imgState.translateX += dx;
            th._imgState.translateY += dy;
            th._applyTransform();
        };
        document.addEventListener('mousemove', th._imgHandlers.mousemove);

        th._imgHandlers.mouseup = function (ev) {
            if (th._imgState.isPanning) {
                th._imgState.isPanning = false;
                th._imgElement.style.cursor = 'grab';
            }
        };
        document.addEventListener('mouseup', th._imgHandlers.mouseup);

        th._imgHandlers.dblclick = function (ev) {
            th._imgState.scale = 1;
            th._imgState.translateX = 0;
            th._imgState.translateY = 0;
            th._applyTransform();
        };
        th._imgElement.addEventListener('dblclick', th._imgHandlers.dblclick);
    };

    th._applyTransform = function () {
        if (!th._imgElement || !th._imgState) return;
        var s = th._imgState;
        th._imgElement.style.transform = 'translate(' + s.translateX + 'px,' + s.translateY + 'px) scale(' + s.scale + ')';
    };

    return th;
})();
