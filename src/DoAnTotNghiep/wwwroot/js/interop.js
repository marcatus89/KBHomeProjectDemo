
export function stopProgressBar() {
  NProgress.done();
}

// Lắng nghe click để hiện progress
export function setupNavigationProgressListener() {
  document.body.addEventListener('click', function(event) {
    const anchor = event.target.closest('a');
    if (anchor && anchor.hasAttribute('href') && anchor.href && anchor.target !== '_blank') {
      const url = new URL(anchor.href);
      if (url.origin === location.origin) {
        if (url.pathname === location.pathname && url.hash) return;
        NProgress.start();
      }
    }
  });
}

// In trang
export function printPage() {
  window.print();
}
window.showBootstrapModal = (id) => {
  try {
    const el = document.getElementById(id);
    if (!el) return;
    // bootstrap should be loaded (bootstrap.bundle.js)
    const modal = bootstrap.Modal.getOrCreateInstance(el);
    modal.show();
  } catch (e) {
    console.error("showBootstrapModal error", e);
  }
};

window.hideBootstrapModal = (id) => {
  try {
    const el = document.getElementById(id);
    if (!el) return;
    const modal = bootstrap.Modal.getOrCreateInstance(el);
    modal.hide();
  } catch (e) {
    console.error("hideBootstrapModal error", e);
  }
};

