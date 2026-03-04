let clickOutsideHandler = null;

export function initLoginDisplay(containerRef, dotNetRef, callbackMethod) {
    if (!containerRef) return;

    const button = containerRef.querySelector('#login-display-button');
    const dropdown = containerRef.querySelector('#login-dropdown');

    if (button && dropdown) {
        button.addEventListener('click', function(e) {
            e.stopPropagation();
            dropdown.classList.toggle('hidden');
        });

        dropdown.addEventListener('click', function(e) {
            e.stopPropagation();
        });

        clickOutsideHandler = function(e) {
            if (!containerRef.contains(e.target)) {
                dropdown.classList.add('hidden');
            }
        };
        document.addEventListener('click', clickOutsideHandler);
    }
}

export function cleanupLoginDisplay() {
    if (clickOutsideHandler) {
        document.removeEventListener('click', clickOutsideHandler);
        clickOutsideHandler = null;
    }
}
