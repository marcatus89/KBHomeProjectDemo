
export function initCarousel(selector) {
    const element = document.querySelector(selector);
    if (element) {
        const carousel = new bootstrap.Carousel(element, {
            interval: 5000,
            ride: 'carousel'
        });
        element._carouselInstance = carousel;
    }
}

export function disposeCarousel(selector) {
    const element = document.querySelector(selector);
    if (element && element._carouselInstance) {
        element._carouselInstance.dispose();
        delete element._carouselInstance;
    }
}
