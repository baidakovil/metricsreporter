using System.Globalization;
using System.Text;

namespace MetricsReporter.Rendering;

/// <summary>
/// Generates CSS styles for the HTML metrics report.
/// </summary>
internal static class HtmlStylesGenerator
{
  private const string ColorScheme = "light";
  private const string DefaultFontStack = "sans-serif";
  private const string PageBackgroundColor = "rgba(235, 235, 235, 1)";
  private const string HeaderBackgroundColor = "rgba(209, 209, 209, 1)";
  private const string HeaderHoverBackgroundColor = "rgba(193, 193, 193, 1)";
  private const string LeafHoverBackgroundColor = "rgba(240, 247, 255, 1)";
  private const string NodeHeaderBackgroundColor = "rgba(221, 221, 221, 1)";
  private const string NodeHeaderFirstColumnBackgroundColor = "rgba(220, 220, 220, 1)";
  private const string LeafBaseBackgroundColor = "rgba(255, 255, 255, 1)";
  private const string LeafStripeOddBackgroundColor = "rgba(243, 243, 243, 1)";
  private const string LeafStripeEvenBackgroundColor = "rgba(255, 255, 255, 1)";
  private const string LeafPrimaryColor = "rgba(204, 0, 0, 1)";

  private const string BadgeNewBackgroundColor = "rgba(26, 127, 55, 1)";
  private const string BadgeNewForegroundColor = "rgba(255, 255, 255, 1)";
  private const string StatusWarningBackgroundColor = "rgba(255, 235, 156, 1)";
  private const string StatusErrorBackgroundColor = "rgba(255, 200, 200, 1)";
  private const string StatusWarningColor = "rgba(182, 111, 26, 1)";
  private const string StatusErrorColor = "rgba(217, 83, 79, 1)";
  private const string DeltaNeutralColor = "rgba(140, 140, 140, 1)";
  private const string IteratorIndicatorColor = "rgba(128, 128, 128, 0.9)";
  private const string SuppressedMetricOverlayColor = "rgba(173, 216, 230, 0.25)";
  private const string SuppressedMetricForegroundColor = "rgba(0, 70, 110, 0.9)";

  private const string DefaultGap = "8px";
  private const string ControlBlockGap = "16px";
  private const string CompactBadgeFontSize = "9px";
  private const string CompactBadgePadding = "3px 6px";
  private const string DeltaSpacing = "4px";
  private const string DetailControlFontSize = "11px";
  private const string DetailControlLabelColor = "rgba(60, 60, 60, 1)";
  private const string SliderTrackColor = "rgba(206, 206, 206, 1)";
  private const string SliderAccentColor = "rgba(0, 102, 204, 1)";
  private const string SliderThumbColor = "rgba(0, 122, 204, 1)";
  private const string SliderHitPadding = "6px 0";
  private const string DetailLabelGap = "3px";
  private const int DetailControlSliderWidth = 62;
  private const int DetailControlSliderMargin = 6;
  private const string ControlInternalGap = "3px";
  private const string PlaceholderExpanderColor = "rgba(140, 140, 140, 1)";
  private const string WarningOverlayColor = "rgba(255, 175, 0, 0.12)";
  private const string ErrorOverlayColor = "rgba(233, 60, 60, 0.14)";
  private const string StateFilterBorderColor = "rgba(185, 185, 185, 1)";
  private const int StateFilterExtraSpacing = 40;

  private const int MaxSupportedDepth = 4;
  private const int ExpanderWidth = 20;
  private const int RootExpanderPadding = ExpanderWidth;
  private const int SymbolColumnWidth = 420;
  private const int BodyMargin = 12;
  private const int WideLayoutMargin = 110;
  private const int SymbolIndentBase = 12;

  private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

  /// <summary>
  /// Generates the complete CSS stylesheet for the metrics report.
  /// </summary>
  /// <returns>The CSS stylesheet as a string.</returns>
  public static string Generate()
  {
    var builder = new StringBuilder(capacity: 4_096);

    AppendRootVariables(builder);
    builder.AppendLine();
    AppendPageLayout(builder);
    builder.AppendLine();
    AppendTypography(builder);
    builder.AppendLine();
    AppendControlPanelStyles(builder);
    builder.AppendLine();
    AppendStateFilterStyles(builder);
    builder.AppendLine();
    AppendDetailControlStyles(builder);
    builder.AppendLine();
    AppendFilterControlStyles(builder);
    builder.AppendLine();
    AppendTableLayout(builder);
    builder.AppendLine();
    AppendColumnGrouping(builder);
    builder.AppendLine();
    AppendTableHeaders(builder);
    builder.AppendLine();
    AppendNodeRowStyles(builder);
    builder.AppendLine();
    AppendTooltipStyles(builder);
    builder.AppendLine();
    AppendMetricStatusStyles(builder);
    builder.AppendLine();
    AppendSymbolColumnStyles(builder);
    builder.AppendLine();
    AppendExpanderStyles(builder);
    builder.AppendLine();
    AppendDepthIndentation(builder);
    builder.AppendLine();
    AppendInteractiveStates(builder);
    builder.AppendLine();
    AppendMetaSectionStyles(builder);

    return builder.ToString();
  }

  private static void AppendRootVariables(StringBuilder builder)
  {
    builder.AppendLine(":root {");
    builder.AppendLine($"  color-scheme: {ColorScheme};");
    builder.AppendLine($"  font-family: {DefaultFontStack};");
    builder.AppendLine("  font-size: 0.9em;");
    builder.AppendLine("  /* Border variables for consistent styling */");
    builder.AppendLine("  --border-dark-color: rgb(175, 175, 175);");
    builder.AppendLine("  --border-dark-width: 1.5px;");
    builder.AppendLine("  --border-light-color: rgb(190, 190, 190);");
    builder.AppendLine("  --border-light-width: 1px;");
    builder.AppendLine("}");
  }

  private static void AppendPageLayout(StringBuilder builder)
  {
    builder.AppendLine("html {");
    builder.AppendLine($"  background-color: {PageBackgroundColor};");
    builder.AppendLine("}");
    builder.AppendLine("body {");
    builder.AppendLine($"  margin: {BodyMargin.ToString(InvariantCulture)}px;");
    builder.AppendLine("  color: rgba(0,0,0,0.92);");
    builder.AppendLine("}");
    builder.AppendLine("@media (min-width: 1201px) {");
    builder.AppendLine("  body {");
    builder.AppendLine($"    margin-top: {BodyMargin.ToString(InvariantCulture)}px;");
    builder.AppendLine($"    margin-bottom: {BodyMargin.ToString(InvariantCulture)}px;");
    builder.AppendLine($"    margin-left: {WideLayoutMargin.ToString(InvariantCulture)}px;");
    builder.AppendLine($"    margin-right: {WideLayoutMargin.ToString(InvariantCulture)}px;");
    builder.AppendLine("  }");
    builder.AppendLine("}");
  }

  private static void AppendTypography(StringBuilder builder)
  {
    builder.AppendLine("h1 { margin-bottom: 4px; }");
    builder.AppendLine(".meta p { margin: 2px 0; }");
    builder.AppendLine(FormattableString.Invariant($".legend {{ margin: 8px 0 12px; display: flex; gap: {DefaultGap}; }}"));
    builder.AppendLine(".badge { padding: 2px 6px; border-radius: 4px; font-size: 11px; font-weight: 600; text-transform: uppercase; }");
    builder.AppendLine(FormattableString.Invariant($".badge-new {{ background: {BadgeNewBackgroundColor}; color: {BadgeNewForegroundColor}; font-size: {CompactBadgeFontSize}; }}"));
    builder.AppendLine(FormattableString.Invariant($".status-warning {{ background: {StatusWarningBackgroundColor}; color: {StatusWarningColor}; font-size: {CompactBadgeFontSize}; padding: {CompactBadgePadding}; }}"));
    builder.AppendLine(FormattableString.Invariant($".status-error {{ background: {StatusErrorBackgroundColor}; color: {StatusErrorColor}; font-size: {CompactBadgeFontSize}; padding: {CompactBadgePadding}; }}"));
    // WHY: Delta colors signal "movement to better", not just positive/negative.
    // Green = improving (moving to better), Red = degrading (moving to worse).
    // Classes are applied by JavaScript based on higherIsBetter flag from threshold data.
    builder.AppendLine(FormattableString.Invariant($".delta-improving {{ color: {BadgeNewBackgroundColor}; margin-left: {DeltaSpacing}; }}"));
    builder.AppendLine(FormattableString.Invariant($".delta-degrading {{ color: {StatusErrorColor}; margin-left: {DeltaSpacing}; }}"));
    // WHY: Some higher-is-worse metrics (like line counts) treat positive deltas as expected growth, so we keep them neutral.
    builder.AppendLine(FormattableString.Invariant($".delta-neutral {{ color: {DeltaNeutralColor}; margin-left: {DeltaSpacing}; }}"));
    // WHY: Iterator/async state-machine coverage is merged back into the user method,
    // but we still want a subtle visual hint that the data originates from a nested compiler-generated type.
    // The indicator is absolutely positioned and centered in the same 20px slot as the expander/placeholder
    // so that member names remain aligned while the glyph does not stick to the cell border.
    builder.AppendLine(".symbol-indicator { position: absolute; left: 0; top: 50%; transform: translateY(-50%); width: 20px; height: 20px; display: flex; align-items: center; justify-content: center; }");
    builder.AppendLine(FormattableString.Invariant($".method-state-machine {{ color: {IteratorIndicatorColor}; font-size: 13px; }}"));
    builder.AppendLine(".fqn { font-family: 'Consolas', 'Courier New', monospace; font-size: 12px; color: rgba(128, 128, 128, 0.8); }");
    // WHY: Coverage links for type nodes (non-leaf) should have black semi-transparent underlining
    // to match the text color while indicating clickability.
    builder.AppendLine(".coverage-link-type { color: inherit; text-decoration: underline; text-decoration-color: rgba(0, 0, 0, 0.3); text-underline-offset: 2px; }");
    builder.AppendLine(".coverage-link-type:visited { color: inherit; }");
    builder.AppendLine(".coverage-link-type:hover { text-decoration-color: rgba(0, 0, 0, 0.6); }");
    // WHY: When type is a leaf row (Detailing = Type), keep red underlining to match member styling.
    builder.AppendLine(FormattableString.Invariant($".metrics tr.leaf-row .coverage-link-type {{ text-decoration-color: rgba(204, 0, 0, 0.3); }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.leaf-row .coverage-link-type:hover {{ text-decoration-color: rgba(204, 0, 0, 0.6); }}"));
  }

  private static void AppendControlPanelStyles(StringBuilder builder)
  {
    builder.AppendLine(".table-container { max-width: 100%; }");
    builder.AppendLine(FormattableString.Invariant($".table-actions {{ display: flex; align-items: center; justify-content: flex-end; flex-wrap: wrap; gap: {ControlBlockGap}; margin: 0; position: sticky; top: 0; background: {PageBackgroundColor}; z-index: 10; padding: 6px 0; }}"));
    builder.AppendLine(FormattableString.Invariant($".status-badges {{ display: flex; gap: {DefaultGap}; align-items: center; }}"));
    builder.AppendLine(".table-actions button { margin-left: 0px; padding: 4px 8px; font-size: 10px; background: rgba(245,245,245,1); border: 1px solid rgba(180,180,180,1); color: rgba(30,30,30,1); border-radius: 4px; cursor: pointer; }");
    builder.AppendLine(".table-actions button:hover { background: rgba(230,230,230,1); }");
  }

  private static void AppendStateFilterStyles(StringBuilder builder)
  {
    builder.AppendLine(FormattableString.Invariant($".state-filters {{ display: flex; align-items: center; gap: {DefaultGap}; padding: 4px 8px; border: 1px solid {StateFilterBorderColor}; border-radius: 4px; background: {PageBackgroundColor}; font-size: {DetailControlFontSize}; color: {DetailControlLabelColor}; margin-right: {StateFilterExtraSpacing}px; }}"));
    builder.AppendLine(".state-filters-label { font-weight: 600; margin-right: 4px; }");
    builder.AppendLine(FormattableString.Invariant($".state-filter-option {{ display: flex; align-items: center; gap: {ControlInternalGap}; font-size: {DetailControlFontSize}; color: inherit; }}"));
    builder.AppendLine(".state-filter-option input[type='checkbox'] { margin: 0; accent-color: rgba(0, 102, 204, 1); }");
  }

  private static void AppendDetailControlStyles(StringBuilder builder)
  {
    builder.AppendLine(FormattableString.Invariant($".detail-control, .awareness-control {{ display: flex; align-items: center; gap: {ControlInternalGap}; font-size: {DetailControlFontSize}; color: {DetailControlLabelColor}; white-space: nowrap; line-height: 1.1; }}"));
    builder.AppendLine(FormattableString.Invariant($".detail-label, .awareness-label {{ font-size: {DetailControlFontSize}; margin-right: {DetailLabelGap}; }}"));
    builder.AppendLine(FormattableString.Invariant($".detail-value, .awareness-value {{ font-size: {DetailControlFontSize}; min-width: 72px; text-align: left; }}"));
    builder.AppendLine(FormattableString.Invariant($".detail-control input[type='range'], .awareness-control input[type='range'] {{ width: {DetailControlSliderWidth}px; margin: 0 {DetailControlSliderMargin}px; height: 4px; accent-color: {SliderAccentColor}; background: {SliderTrackColor}; -webkit-appearance: none; appearance: none; border-radius: 999px; padding: {SliderHitPadding}; box-sizing: content-box; }}"));
    builder.AppendLine(".detail-control input[type='range']::-webkit-slider-thumb, .awareness-control input[type='range']::-webkit-slider-thumb { -webkit-appearance: none; appearance: none; width: 14px; height: 14px; border-radius: 50%; background: " + SliderThumbColor + "; margin-top: -5px; cursor: pointer; box-shadow: none; border: none; }");
    builder.AppendLine(".detail-control input[type='range']::-moz-range-thumb, .awareness-control input[type='range']::-moz-range-thumb { width: 14px; height: 14px; border-radius: 50%; background: " + SliderThumbColor + "; border: none; cursor: pointer; }");
    builder.AppendLine(FormattableString.Invariant($".detail-control input[type='range']::-webkit-slider-runnable-track, .awareness-control input[type='range']::-webkit-slider-runnable-track {{ height: 4px; background: {SliderTrackColor}; border-radius: 999px; }}"));
    builder.AppendLine(FormattableString.Invariant($".detail-control input[type='range']::-moz-range-track, .awareness-control input[type='range']::-moz-range-track {{ height: 4px; background: {SliderTrackColor}; border-radius: 999px; }}"));
  }

  private static void AppendFilterControlStyles(StringBuilder builder)
  {
    builder.AppendLine(".filter-control { display: flex; align-items: center; }");
    builder.AppendLine(".filter-input-wrapper { position: relative; display: inline-block; }");
    builder.AppendLine(".filter-input { width: 100px; padding: 4px 24px 4px 8px; font-size: 11px; border: 1px solid rgba(180,180,180,1); border-radius: 4px; background: rgba(255,255,255,1); color: rgba(30,30,30,1); }");
    builder.AppendLine(".filter-input:focus { outline: none; border-color: rgba(0,102,204,1); }");
    builder.AppendLine(".filter-input::placeholder { color: rgba(128,128,128,0.8); }");
    builder.AppendLine(".filter-clear { position: absolute; right: 4px; top: 50%; transform: translateY(-50%); background: none; border: none; color: rgba(128,128,128,0.8); font-size: 18px; line-height: 1; cursor: pointer; padding: 0; width: 18px; height: 18px; display: flex; align-items: center; justify-content: center; }");
    builder.AppendLine(".filter-clear:hover { color: rgba(30,30,30,1); }");
    builder.AppendLine(".filter-clear:focus { outline: none; color: rgba(0,102,204,1); }");
  }

  private static void AppendTableLayout(StringBuilder builder)
  {
    builder.AppendLine(".metrics { border-collapse: collapse; width: 100%; table-layout: fixed; word-wrap: break-word; border-spacing: 0; font-size: 0.9em; border: var(--border-dark-width) solid var(--border-dark-color); }");
    builder.AppendLine(".metrics th, .metrics td { border-right: var(--border-light-width) solid var(--border-light-color); border-bottom: var(--border-light-width) solid var(--border-light-color); border-top: none; border-left: none; padding: 1px 3px; vertical-align: middle; line-height: 1.3; height: auto; -webkit-hyphens: auto; -ms-hyphens: auto; hyphens: auto; }");
    builder.AppendLine(".metrics th:first-child, .metrics td:first-child { border-left: var(--border-dark-width) solid var(--border-dark-color); }");
    builder.AppendLine(".metrics th:last-child, .metrics td:last-child { border-right: var(--border-dark-width) solid var(--border-dark-color); }");
    builder.AppendLine(".metrics tbody tr:first-child td, .metrics tbody tr:first-child th { border-top: var(--border-dark-width) solid var(--border-dark-color) !important; }");
    builder.AppendLine(".metrics tbody tr:last-child td, .metrics tbody tr:last-child th { border-bottom: var(--border-dark-width) solid var(--border-dark-color); }");
  }

  private static void AppendColumnGrouping(StringBuilder builder)
  {
    builder.AppendLine("/* Column group separators */");
    builder.AppendLine(".metrics th[data-col='symbol'], .metrics td.symbol, .metrics th.symbol { border-right: var(--border-dark-width) solid var(--border-dark-color); }");
    builder.AppendLine(".metrics thead th[data-col='symbol'] { border-bottom: var(--border-dark-width) solid var(--border-dark-color) !important; }");

    builder.AppendLine(".metrics th[data-col='OpenCoverCyclomaticComplexity'], .metrics td.metric[data-col='OpenCoverCyclomaticComplexity'], .metrics th.metric[data-col='OpenCoverCyclomaticComplexity'] { border-right: var(--border-dark-width) solid var(--border-dark-color); }");
    builder.AppendLine(".metrics th[data-col-group='OpenCover'] { border-right: var(--border-dark-width) solid var(--border-dark-color); }");

    builder.AppendLine(".metrics th[data-col='RoslynCyclomaticComplexity'], .metrics td.metric[data-col='RoslynCyclomaticComplexity'], .metrics th.metric[data-col='RoslynCyclomaticComplexity'] { border-left: var(--border-dark-width) solid var(--border-dark-color); }");
    builder.AppendLine(".metrics th[data-col='RoslynExecutableLines'], .metrics td.metric[data-col='RoslynExecutableLines'], .metrics th.metric[data-col='RoslynExecutableLines'] { border-right: var(--border-dark-width) solid var(--border-dark-color); }");
    builder.AppendLine(".metrics th[data-col-group='Roslyn'] { border-left: var(--border-dark-width) solid var(--border-dark-color); border-right: var(--border-dark-width) solid var(--border-dark-color); }");

    builder.AppendLine(".metrics th[data-col='SarifCaRuleViolations'], .metrics td.metric[data-col='SarifCaRuleViolations'], .metrics th.metric[data-col='SarifCaRuleViolations'] { border-left: var(--border-dark-width) solid var(--border-dark-color); }");
    builder.AppendLine(".metrics th[data-col-group='Sarif'] { border-left: var(--border-dark-width) solid var(--border-dark-color); }");
  }

  private static void AppendTableHeaders(StringBuilder builder)
  {
    builder.AppendLine(FormattableString.Invariant($".metrics thead th {{ border-top: var(--border-dark-width) solid var(--border-dark-color); background-color: {HeaderBackgroundColor}; text-align: left; white-space: normal; word-wrap: break-word; cursor: pointer; position: sticky; top: 40px; z-index: 5; will-change: transform; -webkit-hyphens: auto; -ms-hyphens: auto; hyphens: auto; }}"));
    builder.AppendLine(".metrics thead th[data-col='symbol'] { padding-left: 8px; }");
    builder.AppendLine(".metrics thead tr:first-child th { padding-top: 8px; padding-bottom: 8px; box-shadow: 0 calc(-1 * var(--border-dark-width)) 0 0 var(--border-dark-color); }");
    builder.AppendLine(".metrics thead tr:first-child th:not([data-col='symbol']) { text-align: center; font-weight: bold; }");
    builder.AppendLine(".metrics thead tr:nth-child(2) th { box-shadow: 0 calc(-1 * var(--border-light-width)) 0 0 var(--border-light-color); }");
    builder.AppendLine(".metrics thead tr:last-child th { border-bottom: var(--border-dark-width) solid var(--border-dark-color) !important; }");
    builder.AppendLine(".metrics thead th.sort-asc::after { content: '▲'; font-size: 10px; margin-left: 4px; display: inline-block; }");
    builder.AppendLine(".metrics thead th.sort-desc::after { content: '▼'; font-size: 10px; margin-left: 4px; display: inline-block; }");
  }

  private static void AppendNodeRowStyles(StringBuilder builder)
  {
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-header th {{ background-color: {NodeHeaderBackgroundColor}; font-weight: bold; color: rgba(0, 0, 0, 1); text-align: left; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-header th:first-child {{ background-color: {NodeHeaderFirstColumnBackgroundColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-item td {{ text-align: left; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; background-color: {LeafBaseBackgroundColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-item.stripe-odd td {{ background-color: {LeafStripeOddBackgroundColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-item.stripe-even td {{ background-color: {LeafStripeEvenBackgroundColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-item td.symbol .item-name {{ color: {LeafPrimaryColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.leaf-row th, .metrics tr.leaf-row td {{ background-color: {LeafBaseBackgroundColor}; color: rgba(0, 0, 0, 1); font-weight: normal; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.leaf-row.stripe-odd th, .metrics tr.leaf-row.stripe-odd td {{ background-color: {LeafStripeOddBackgroundColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.leaf-row.stripe-even th, .metrics tr.leaf-row.stripe-even td {{ background-color: {LeafStripeEvenBackgroundColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.leaf-row .symbol .name-text {{ color: {LeafPrimaryColor}; }}"));
  }

  private static void AppendMetricStatusStyles(StringBuilder builder)
  {
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-header th.metric[data-status='warning'] {{ box-shadow: inset 0 0 0 9999px {WarningOverlayColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-header th.metric[data-status='error'] {{ box-shadow: inset 0 0 0 9999px {ErrorOverlayColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-item td.metric[data-status='warning'] {{ box-shadow: inset 0 0 0 9999px {WarningOverlayColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-item td.metric[data-status='error'] {{ box-shadow: inset 0 0 0 9999px {ErrorOverlayColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-item td.metric[data-status='error'] .metric-value {{ color: {StatusErrorColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-item td.metric[data-status='warning'] .metric-value {{ color: {StatusWarningColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-header th.metric[data-status='error'] .metric-value {{ color: {StatusErrorColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-header th.metric[data-status='warning'] .metric-value {{ color: {StatusWarningColor}; }}"));
    builder.AppendLine(".metrics tr.node-header th .metric-value { font-weight: bold; }");
    builder.AppendLine(".metrics tr.node-item td .metric-value { font-weight: normal; }");
    builder.AppendLine(".metrics tr.leaf-row th.metric .metric-value { font-weight: normal; }");
    builder.AppendLine(FormattableString.Invariant($".metrics tr.leaf-row .metric[data-status='warning'] {{ box-shadow: inset 0 0 0 9999px {WarningOverlayColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.leaf-row .metric[data-status='error'] {{ box-shadow: inset 0 0 0 9999px {ErrorOverlayColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.leaf-row .metric[data-status='error'] .metric-value {{ color: {StatusErrorColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.leaf-row .metric[data-status='warning'] .metric-value {{ color: {StatusWarningColor}; }}"));
    builder.AppendLine(".metrics tr.leaf-row .metric-value { font-weight: normal; }");
    builder.AppendLine(".metric-value { font-weight: normal; }");
    // WHY: Suppressed metrics should remain visible but de-emphasised and clearly
    // distinguished from both normal, warning and error states. A light azure,
    // semi-transparent overlay keeps the cell readable while signalling that the
    // high severity is intentionally ignored via SuppressMessage.
    // Suppressed styles must have equal or greater specificity than error/warning
    // styles to override them when both attributes are present.
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-header th.metric[data-suppressed='true'] {{ box-shadow: inset 0 0 0 9999px {SuppressedMetricOverlayColor} !important; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-header th.metric[data-suppressed='true'] .metric-value {{ color: {SuppressedMetricForegroundColor} !important; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-item td.metric[data-suppressed='true'] {{ box-shadow: inset 0 0 0 9999px {SuppressedMetricOverlayColor} !important; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.node-item td.metric[data-suppressed='true'] .metric-value {{ color: {SuppressedMetricForegroundColor} !important; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.leaf-row .metric[data-suppressed='true'] {{ box-shadow: inset 0 0 0 9999px {SuppressedMetricOverlayColor} !important; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr.leaf-row .metric[data-suppressed='true'] .metric-value {{ color: {SuppressedMetricForegroundColor} !important; }}"));
  }

  private static void AppendSymbolColumnStyles(StringBuilder builder)
  {
    builder.AppendLine($"th[data-col='symbol'], td.symbol, th.symbol {{ width: {SymbolColumnWidth.ToString(InvariantCulture)}px; box-sizing: border-box; }}");
    builder.AppendLine(FormattableString.Invariant($".symbol {{ position: relative; width: {SymbolColumnWidth.ToString(InvariantCulture)}px; white-space: nowrap; overflow: hidden; box-sizing: border-box; line-height: inherit; padding-right: 48px; }}"));
    builder.AppendLine(".metrics thead th:not([data-col='symbol']), .metrics td.metric, .metrics th.metric { width: auto; }");
    builder.AppendLine(FormattableString.Invariant($".symbol .name-text {{ display: inline-block; vertical-align: middle; max-width: 100%; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; min-width: 0; padding-right: 40px; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tr[data-role='member'] .symbol .name-text {{ margin-left: {ExpanderWidth.ToString(InvariantCulture)}px; }}"));
    builder.AppendLine(".symbol .row-action-icons { position: absolute; top: 50%; right: 6px; transform: translateY(-50%); display: flex; gap: 4px; opacity: 0; transition: opacity 0.18s ease; transition-delay: 0s; }");
    builder.AppendLine("tr.node-row:hover .row-action-icons, tr.node-row:focus-within .row-action-icons { opacity: 1; transition-delay: 0.25s; }");
    builder.AppendLine(".row-action-icon { min-width: 16px; border: none; background: transparent; color: rgba(96, 96, 96, 0.25); font-size: 12px; line-height: 1; padding: 0 2px; cursor: pointer; text-align: center; }");
    builder.AppendLine(".row-action-icon:focus-visible { outline: none; }");
    builder.AppendLine(".row-action-icon:hover { color: rgba(30, 30, 30, 0.5); }");
  }

  private static void AppendExpanderStyles(StringBuilder builder)
  {
    builder.AppendLine(".expander { position: absolute; left: 0; top: 50%; transform: translateY(-50%); border: 0; background: transparent; cursor: pointer; font-size: 14px; line-height: 1; width: 20px; height: 20px; display: flex; align-items: center; justify-content: center; z-index: 1; padding: 0; margin: 0; user-select: none; font-weight: bold; }");
    builder.AppendLine(".expander:focus { outline: 1px solid rgba(0,0,0,0.3); outline-offset: 2px; border-radius: 2px; }");
    builder.AppendLine(FormattableString.Invariant($".expander-placeholder {{ font-weight: bold; color: {PlaceholderExpanderColor}; pointer-events: auto; user-select: none; }}"));
  }

  private static void AppendDepthIndentation(StringBuilder builder)
  {
    builder.AppendLine("/* Symbol indentation by tree level */");
    builder.AppendLine("tr.node-row[data-level='0'] .symbol { padding-left: 0; }");
    builder.AppendLine($"tr.node-row[data-level='0'] .symbol.has-expander {{ padding-left: {RootExpanderPadding.ToString(InvariantCulture)}px; }}");

    for (var level = 1; level <= MaxSupportedDepth; level++)
    {
      var basePadding = level * SymbolIndentBase;
      var withExpander = basePadding + ExpanderWidth;
      builder.AppendLine(FormattableString.Invariant($"tr.node-row[data-level='{level}'] .symbol {{ padding-left: {withExpander}px; }}"));
      builder.AppendLine(FormattableString.Invariant($"tr.node-row[data-level='{level}'] .symbol:not(.has-expander) {{ padding-left: {basePadding}px; }}"));
    }
  }

  private static void AppendInteractiveStates(StringBuilder builder)
  {
    builder.AppendLine(FormattableString.Invariant($".metrics tbody tr.node-item:hover td {{ background-color: {LeafHoverBackgroundColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tbody tr.node-header:hover th {{ background-color: {HeaderHoverBackgroundColor}; }}"));
    builder.AppendLine(FormattableString.Invariant($".metrics tbody tr.leaf-row:hover td, .metrics tbody tr.leaf-row:hover th {{ background-color: {LeafHoverBackgroundColor}; }}"));
  }

  private static void AppendMetaSectionStyles(StringBuilder builder)
  {
    builder.AppendLine(".meta-summary { cursor: pointer; user-select: none; }");
    builder.AppendLine(".meta-summary:hover { opacity: 0.7; }");
    builder.AppendLine(".meta-toggle { display: inline-block; transition: transform 0.2s ease; margin-left: 4px; }");
    builder.AppendLine(".meta-summary.expanded .meta-toggle { transform: rotate(90deg); }");
    builder.AppendLine(".meta-details { max-height: 0; overflow: hidden; opacity: 0; transition: max-height 0.25s ease, opacity 0.15s ease; }");
    builder.AppendLine(".meta-details.expanded { max-height: 1200px; opacity: 1; }");
    builder.AppendLine(".hotkeys-summary { background: rgba(255, 255, 255, 0.92); font-size: inherit; line-height: 1.3; font-weight: 400; }");
    builder.AppendLine(".hotkeys-summary p { margin: 2px 0; display: flex; flex-wrap: wrap; gap: 12px; }");
    builder.AppendLine(".hotkey-pair { display: inline-flex; align-items: center; gap: 6px; }");
    builder.AppendLine(".hotkey-pair + .hotkey-pair::before { content: ''; width: 8px; height: 8px; border-radius: 50%; background: rgba(0, 0, 0, 0.25); display: inline-block; margin: 0 12px; }");
    builder.AppendLine(".meta-details { margin-top: 8px; margin-bottom: 20px; }");
    builder.AppendLine(".meta-section { background: rgb(245, 245, 245); border: 1px solid rgba(0, 0, 0, 0.12); border-radius: 10px; padding: 8px 12px; margin: 10px 0 4px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.05); }");
    builder.AppendLine(".meta-section .section-title { font-size: 12px; letter-spacing: 0.08em; text-transform: uppercase; color: rgba(0, 0, 0, 0.65); margin-bottom: 6px; }");
    //builder.AppendLine(".meta-section-divider { width: 32%; height: 1px; margin: 10px auto; background: rgba(0, 0, 0, 0.18); border-radius: 999px; }");
  }

  private static void AppendTooltipStyles(StringBuilder builder)
  {
    builder.AppendLine(".metric-tooltip { position: absolute; z-index: 1200; background: rgba(245, 245, 245, 0.98); color: #1a1a1a; padding: 10px 12px; border-radius: 6px; max-width: 384px; font-size: 12px; line-height: 1.4; box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15); pointer-events: none; border: 1px solid rgba(0, 0, 0, 0.1); word-wrap: break-word; overflow-wrap: break-word; }");
    builder.AppendLine(".metric-tooltip__desc { margin: 0 0 4px 0; font-size: 12px; color: #333; word-wrap: break-word; overflow-wrap: break-word; }");
    builder.AppendLine(".metric-tooltip__direction { margin: 0 0 6px 0; font-size: 12px; color: #555; }");
    builder.AppendLine(".metric-tooltip__heading { margin: 0 0 4px 0; font-size: 11px; letter-spacing: 0.02em; text-transform: uppercase; color: #222; font-weight: 600; }");
    builder.AppendLine(".metric-tooltip__list { margin: 0; padding: 0; list-style: none; }");
    builder.AppendLine(".metric-tooltip__list li { margin: 0; padding: 0; display: flex; gap: 6px; }");
    builder.AppendLine(".metric-tooltip__list li span { color: #0066cc; font-variant-numeric: tabular-nums; }");
    builder.AppendLine(".metric-tooltip strong { font-weight: 600; color: #1a1a1a; }");
    builder.AppendLine(".metric-tooltip em { font-style: italic; }");
    builder.AppendLine(".metric-tooltip code { display: inline-block; font-family: 'Consolas','Courier New',monospace; font-size: 11px; white-space: pre-wrap; word-break: break-all; overflow-wrap: anywhere; }");
  }
}


