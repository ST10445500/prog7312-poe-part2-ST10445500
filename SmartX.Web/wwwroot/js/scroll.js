// brings an element to the top of the view, the element's css scroll-margin keeps it clear of the sticky bars
export function scrollToElement(element) {
    if (!element) {
        return;
    }

    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    element.scrollIntoView({ block: 'start', behavior: reduce ? 'auto' : 'smooth' });
}
