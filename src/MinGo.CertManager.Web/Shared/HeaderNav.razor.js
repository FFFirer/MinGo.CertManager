let clickOutsideHandler = null;

export function registerClickOutside(elementRef, dotNetRef, callbackMethod) {
    clickOutsideHandler = (event) => {
        if (elementRef && !elementRef.contains(event.target)) {
            dotNetRef.invokeMethodAsync(callbackMethod);
        }
    };
    
    document.addEventListener('click', clickOutsideHandler);
}

export function unregisterClickOutside() {
    if (clickOutsideHandler) {
        document.removeEventListener('click', clickOutsideHandler);
        clickOutsideHandler = null;
    }
}
