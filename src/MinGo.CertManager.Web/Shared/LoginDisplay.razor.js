let clickOutsideHandler = null;

export function initLoginDisplay(containerRef, dotNetRef, callbackMethod) {
    if (!containerRef) return;

    // 查找用户菜单容器
    const userMenuContainer = containerRef;
    
    if (userMenuContainer && dotNetRef && callbackMethod) {
        // 点击外部关闭菜单的处理
        clickOutsideHandler = function(e) {
            if (!userMenuContainer.contains(e.target)) {
                dotNetRef.invokeMethodAsync(callbackMethod);
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
