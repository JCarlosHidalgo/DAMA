/*
 * Highlights nodes inside Doxygen-generated inheritance graphs so the reader
 * can tell at a glance which types belong to the project and which come
 * from outside (BCL, ASP.NET Core, third-party packages).
 *
 * Doxygen 1.17 wraps every dot-generated SVG in an <iframe>. That iframe is
 * a separate document, so the parent's stylesheet does not cascade into it
 * — styling has to be inserted from the inside. This script:
 *
 *   1. Classifies each node by inspecting its xlink:href and label. Doxygen
 *      links the project's own types to `interface*.html` or `class*.html`
 *      (hierarchy-graph variants append a `-N-g` suffix, so the name match
 *      allows `-`). External types have no Doxygen page and therefore no
 *      link, so they are recognised by their label: the .NET I-prefix
 *      convention separates external interfaces from external classes.
 *      The buckets and their styles:
 *        - own interfaces      → solid cream/dark fill, dashed amber border
 *        - external interfaces → yellow diagonal stripes
 *        - external classes    → cyan diagonal stripes
 *        - own classes         → Doxygen's default look
 *   2. Injects `<defs>` (for the stripe `<pattern>`s) and `<style>` into
 *      each iframe's SVG document so the rules that depend on the tag
 *      actually apply.
 *   3. Re-injects when the user toggles Doxygen's dark mode.
 */
(function () {
  const SVG_NS = 'http://www.w3.org/2000/svg';
  const XLINK_NS = 'http://www.w3.org/1999/xlink';
  const INTERFACE_HREF_PATTERN = /(^|\/)interface[^/.]+\.html/;
  const CLASS_HREF_PATTERN = /(^|\/)class[^/.]+\.html/;
  /*
   * DAMA's own code lives under the `Backend.*` namespace (every backend, no
   * exception). Doxygen mangles `Backend.` to `Backend_1_1` in filenames, so
   * `(class|interface)Backend_1_1…` reliably marks an own type. A plain `_1_1`
   * check is not enough because Doxygen sometimes namespace-mangles external
   * types too (e.g. interfaceSystem_1_1IDisposable.html), which would falsely
   * read as own.
   */
  const OWN_HREF_PATTERN = /(^|\/)(class|interface)Backend_1_1/;
  /*
   * Fallback for nodes Doxygen did not link (external/BCL types like
   * IDisposable, IAsyncActionFilter): .NET interfaces follow the I-prefix
   * convention with a second uppercase letter. The leading `[.:]` covers
   * namespace-qualified labels (`System.IDisposable`, `Microsoft::…::IHostedService`).
   */
  const INTERFACE_NAME_PATTERN = /(^|[.:])I[A-Z]/;
  const STYLE_ID = 'interface-highlight-style';
  const DEFS_ID = 'interface-highlight-defs';

  /*
   * UML-look (per-class) graphs render each node as several stacked polygons:
   *   - first polygon: the background rectangle (fill="white")
   *   - middle polygons: compartment separator lines (fill="#666666")
   *   - last polygon: the outline border (fill="none", drawn on top of text)
   * The simple hierarchy graph only emits one polygon per node.
   *
   * Filling every polygon paints the outline over the text. So we tint only
   * the first polygon and restyle only the stroke of the last polygon —
   * leaving its fill="none" untouched so the text below stays visible.
   */
  const LIGHT_STYLE = `
    g.node-interface polygon:first-of-type {
      fill: #fef5e7 !important;
      stroke: #c05621 !important;
      stroke-width: 2.5px !important;
      stroke-dasharray: 6 3 !important;
    }
    g.node-interface polygon:last-of-type {
      stroke: #c05621 !important;
      stroke-width: 2.5px !important;
      stroke-dasharray: 6 3 !important;
    }
    g.node-interface text {
      fill: #7c2d12 !important;
      font-style: italic;
      font-weight: bold;
    }
    g.node-external-interface polygon:first-of-type,
    polygon.orphan-external-interface {
      fill: url(#interface-stripes) !important;
      fill-opacity: 0.6 !important;
      stroke: #b7791f !important;
      stroke-opacity: 0.6 !important;
      stroke-width: 2.5px !important;
      stroke-dasharray: 6 3 !important;
    }
    g.node-external-interface polygon:last-of-type {
      stroke: #b7791f !important;
      stroke-opacity: 0.6 !important;
      stroke-width: 2.5px !important;
      stroke-dasharray: 6 3 !important;
    }
    g.node-external-interface text,
    text.orphan-external-interface {
      fill: #7c2d12 !important;
      font-style: italic;
      font-weight: bold;
    }
    g.node-external-class polygon:first-of-type,
    polygon.orphan-external-class {
      fill: url(#class-stripes) !important;
      fill-opacity: 0.6 !important;
      stroke: none !important;
    }
    g.node-external-class polygon:last-of-type {
      stroke: none !important;
    }
    g.node-external-class text,
    text.orphan-external-class {
      fill: #234e52 !important;
    }
  `;

  const DARK_STYLE = `
    g.node-interface polygon:first-of-type {
      fill: #3b2a0f !important;
      stroke: #f6ad55 !important;
      stroke-width: 2.5px !important;
      stroke-dasharray: 6 3 !important;
    }
    g.node-interface polygon:last-of-type {
      stroke: #f6ad55 !important;
      stroke-width: 2.5px !important;
      stroke-dasharray: 6 3 !important;
    }
    g.node-interface text {
      fill: #fbd38d !important;
      font-style: italic;
      font-weight: bold;
    }
    g.node-external-interface polygon:first-of-type,
    polygon.orphan-external-interface {
      fill: url(#interface-stripes) !important;
      fill-opacity: 0.6 !important;
      stroke: #f6ad55 !important;
      stroke-opacity: 0.6 !important;
      stroke-width: 2.5px !important;
      stroke-dasharray: 6 3 !important;
    }
    g.node-external-interface polygon:last-of-type {
      stroke: #f6ad55 !important;
      stroke-opacity: 0.6 !important;
      stroke-width: 2.5px !important;
      stroke-dasharray: 6 3 !important;
    }
    g.node-external-interface text,
    text.orphan-external-interface {
      fill: #fbd38d !important;
      font-style: italic;
      font-weight: bold;
    }
    g.node-external-class polygon:first-of-type,
    polygon.orphan-external-class {
      fill: url(#class-stripes) !important;
      fill-opacity: 0.6 !important;
      stroke: none !important;
    }
    g.node-external-class polygon:last-of-type {
      stroke: none !important;
    }
    g.node-external-class text,
    text.orphan-external-class {
      fill: #b2f5ea !important;
    }
  `;

  function isDarkMode() {
    return document.documentElement.classList.contains('dark-mode');
  }

  /*
   * Classify a node in one of three buckets:
   *   - 'node-interface'           — the project's own interface (Doxygen
   *                                  linked it to interface*.html).
   *   - 'node-external-interface'  — no Doxygen page and the label matches
   *                                  the .NET I-prefix convention; the type
   *                                  is from the BCL / a framework package.
   *   - 'node-external-class'      — no Doxygen page and the label does not
   *                                  look like an interface; an external
   *                                  base class (Controller, BackgroundService).
   * The project's own classes return null and keep Doxygen's default look.
   */
  function classifyNode(node) {
    const link = node.querySelector('a');
    const href = link
      ? (link.getAttributeNS(XLINK_NS, 'href') ||
         link.getAttribute('xlink:href') ||
         link.getAttribute('href') ||
         '')
      : '';
    /*
     * Empty href = focus node of a per-class diagram. Doxygen never self-links:
     * sometimes it omits <a> entirely, sometimes it keeps <a xlink:title=" ">
     * for the tooltip but drops xlink:href. The diagram's host HTML page url
     * still tells us whether that focus is an own type (Doxygen mangles
     * namespace separators as `_1_1`) or an external stub (bare classFoo.html).
     */
    if (!href) {
      return classifyByHostPage(node);
    }
    /*
     * The Backend_1_1 prefix in the href (own DAMA namespace) is the primary
     * own/external boundary. Doxygen stubs external types as class*.html with
     * EXTRACT_ALL=YES, and may even namespace-mangle them, so without that
     * prefix check the `_1_1` mangling alone would falsely tag external types
     * with namespace as own. The I-prefix on the label catches external
     * interfaces Doxygen mislabeled as `class*.html` stubs (IAsyncActionFilter
     * et al.).
     */
    if (INTERFACE_HREF_PATTERN.test(href)) {
      return OWN_HREF_PATTERN.test(href) ? 'node-interface' : 'node-external-interface';
    }
    if (CLASS_HREF_PATTERN.test(href)) {
      if (OWN_HREF_PATTERN.test(href)) {
        return null;
      }
      return INTERFACE_NAME_PATTERN.test(readNodeLabel(node))
        ? 'node-external-interface'
        : 'node-external-class';
    }
    return 'node-external-class';
  }

  function classifyByHostPage(node) {
    const basename = (window.location.pathname || '').split('/').pop() || '';
    if (OWN_HREF_PATTERN.test(basename)) {
      // Own type's focus — keep the highlight only for interfaces; own
      // classes fall back to Doxygen's default look.
      return INTERFACE_HREF_PATTERN.test(basename) ? 'node-interface' : null;
    }
    const name = readNodeLabel(node);
    if (INTERFACE_HREF_PATTERN.test(basename) || INTERFACE_NAME_PATTERN.test(name)) {
      return 'node-external-interface';
    }
    if (CLASS_HREF_PATTERN.test(basename)) {
      return 'node-external-class';
    }
    return null;
  }

  function readNodeLabel(node) {
    const labels = node.querySelectorAll('text');
    for (const label of labels) {
      const candidate = (label.textContent || '').trim();
      if (candidate && candidate[0] !== '«' && candidate[0] !== '<') {
        return candidate;
      }
    }
    return '';
  }

  function tagNodes(svgDoc) {
    svgDoc.querySelectorAll('g.node').forEach(function (node) {
      const kind = classifyNode(node);
      if (kind) {
        node.classList.add(kind);
      }
    });
    tagOrphanNodes(svgDoc);
  }

  /*
   * Some Doxygen layouts emit nodes that are NOT wrapped in <g class="node">
   * (e.g. external/unlinked types in the full graphical hierarchy). Pick them
   * up by scanning every <text> outside `g.node`, pairing it with its previous
   * <polygon> sibling, and classifying by the same I-prefix rule. CSS targets
   * those polygons and texts directly via `polygon.orphan-*` / `text.orphan-*`.
   */
  function tagOrphanNodes(svgDoc) {
    svgDoc.querySelectorAll('text').forEach(function (textElement) {
      if (textElement.closest('g.node')) return;
      const name = (textElement.textContent || '').trim();
      if (!name || name[0] === '«' || name[0] === '<') return;
      let polygon = textElement.previousElementSibling;
      while (polygon && polygon.tagName.toLowerCase() !== 'polygon') {
        polygon = polygon.previousElementSibling;
      }
      if (!polygon) return;
      const kind = INTERFACE_NAME_PATTERN.test(name)
        ? 'orphan-external-interface'
        : 'orphan-external-class';
      polygon.classList.add(kind);
      textElement.classList.add(kind);
    });
  }

  /*
   * Stripe fill for external types. SVG does not honour CSS `background`
   * or `repeating-linear-gradient` on `<polygon>`, so a `<pattern>` is
   * defined per SVG document and referenced from CSS as `fill: url(#…)`.
   */
  function createStripePattern(svgDoc, id, baseFill, stripeColor) {
    const pattern = svgDoc.createElementNS(SVG_NS, 'pattern');
    pattern.setAttribute('id', id);
    pattern.setAttribute('patternUnits', 'userSpaceOnUse');
    pattern.setAttribute('width', '8');
    pattern.setAttribute('height', '8');
    pattern.setAttribute('patternTransform', 'rotate(45)');

    const background = svgDoc.createElementNS(SVG_NS, 'rect');
    background.setAttribute('width', '8');
    background.setAttribute('height', '8');
    background.setAttribute('fill', baseFill);
    pattern.appendChild(background);

    const stripe = svgDoc.createElementNS(SVG_NS, 'line');
    stripe.setAttribute('x1', '0');
    stripe.setAttribute('y1', '0');
    stripe.setAttribute('x2', '0');
    stripe.setAttribute('y2', '8');
    stripe.setAttribute('stroke', stripeColor);
    stripe.setAttribute('stroke-width', '3');
    pattern.appendChild(stripe);

    return pattern;
  }

  function injectDefs(svgDoc) {
    const existing = svgDoc.getElementById(DEFS_ID);
    if (existing) {
      existing.remove();
    }
    const defs = svgDoc.createElementNS(SVG_NS, 'defs');
    defs.setAttribute('id', DEFS_ID);
    const palette = isDarkMode()
      ? { interfaceBase: '#3b2a0f', interfaceStripe: '#f6ad55',
          classBase:     '#0f2e2e', classStripe:     '#4fd1c5' }
      : { interfaceBase: '#fef5e7', interfaceStripe: '#d69e2e',
          classBase:     '#e6fffa', classStripe:     '#319795' };
    defs.appendChild(createStripePattern(svgDoc, 'interface-stripes',
                                         palette.interfaceBase, palette.interfaceStripe));
    defs.appendChild(createStripePattern(svgDoc, 'class-stripes',
                                         palette.classBase, palette.classStripe));
    svgDoc.documentElement.insertBefore(defs, svgDoc.documentElement.firstChild);
  }

  function injectStyle(svgDoc) {
    let style = svgDoc.getElementById(STYLE_ID);
    if (!style) {
      style = svgDoc.createElementNS(SVG_NS, 'style');
      style.setAttribute('id', STYLE_ID);
      svgDoc.documentElement.insertBefore(style, svgDoc.documentElement.firstChild);
    }
    style.textContent = isDarkMode() ? DARK_STYLE : LIGHT_STYLE;
  }

  function processSvgDocument(svgDoc) {
    if (!svgDoc || !svgDoc.documentElement) return;
    if (svgDoc.documentElement.tagName.toLowerCase() !== 'svg') return;
    tagNodes(svgDoc);
    injectDefs(svgDoc);
    injectStyle(svgDoc);
  }

  function attachToIframe(iframe) {
    function handle() {
      try { processSvgDocument(iframe.contentDocument); } catch (_) {}
    }
    iframe.addEventListener('load', handle);
    // Lazy-loaded iframes may not have fired `load` yet, but if the SVG is
    // already there (e.g. after dark-mode toggle re-runs us) process now.
    handle();
  }

  /*
   * Class and interface detail pages wrap the inheritance graph in a
   * collapsible <div class="dynheader closed">. We open it on load so the
   * diagram is visible right away (and the lazy iframe starts loading).
   */
  function expandInheritanceDiagrams(doc) {
    doc.querySelectorAll('iframe[src*="__inherit__graph.svg"]').forEach(function (iframe) {
      const content = iframe.closest('.dyncontent');
      if (!content || !content.id) return;
      const header = doc.getElementById(content.id.replace(/-content$/, ''));
      if (!header || !header.classList.contains('closed')) return;
      // The inline onclick is `return dynsection.toggleVisibility(this)`;
      // a synthetic click keeps the page's own toggle bookkeeping correct.
      header.click();
    });
  }

  function processDocument(doc) {
    expandInheritanceDiagrams(doc);
    doc.querySelectorAll('iframe[src$=".svg"]').forEach(attachToIframe);
    // Legacy <object> path, kept for older Doxygen builds.
    doc.querySelectorAll('object[type="image/svg+xml"], object[data$=".svg"]').forEach(function (obj) {
      function handle() {
        try { processSvgDocument(obj.contentDocument); } catch (_) {}
      }
      obj.addEventListener('load', handle);
      handle();
    });
    // Inline SVGs (also legacy).
    doc.querySelectorAll('svg').forEach(tagNodes);
  }

  function refreshAll() {
    document.querySelectorAll('iframe[src$=".svg"]').forEach(function (iframe) {
      try { processSvgDocument(iframe.contentDocument); } catch (_) {}
    });
  }

  new MutationObserver(function (mutations) {
    for (const m of mutations) {
      if (m.attributeName === 'class') {
        refreshAll();
        return;
      }
    }
  }).observe(document.documentElement, { attributes: true });

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () { processDocument(document); });
  } else {
    processDocument(document);
  }
})();
