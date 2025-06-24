window.addToastAnimation = (elementId, animationClass) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.classList.add(animationClass);
    }
};