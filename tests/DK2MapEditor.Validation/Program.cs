using System.Buffers.Binary;
using System.Text;
using System.Xml.Linq;
using DK2MapEditor.Core.Diagnostics;
using DK2MapEditor.Core.Formats;
using DK2MapEditor.Core.Editing;

var failures = new List<string>();
var passedChecks = new List<string>();
string? reportPath = null;
if (args.Length >= 2 && args[^2] == "--report") { reportPath = Path.GetFullPath(args[^1]); args = args[..^2]; }
var inspector = new Dk2FileInspector();
if(args.Length is 1 or 2 && args[0]=="--project-copy-source-archive-only")
{
    var project=args.Length==2 ? args[1] : Path.Combine(FindRepoRoot(),"tests/fixtures/NativeRoundtrip/DK2Preview_6a88240eaf814f78.dkm");
    Run("Export Copy keeps the native source archive and current DKM edits across reopen and normal Save",()=>ProjectCopySourceArchiveChecks.Run(project));
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--owner-scope-existing-only"}))
{
    OwnerScopeNamespaceChecks.RunExisting(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--selection-unrelated-script-ids-only"}))
{
    SelectionUnrelatedScriptIdentityChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--owner-scope-compilation-only"}))
{
    OwnerScopeNamespaceChecks.RunCompilation(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--owner-scope-namespaces-only"}))
{
    OwnerScopeNamespaceChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--gate-transform-semantics-only"}))
{
    GateTransformSemanticsChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--selection-physical-payloads-only","--native-only"}))
{
    SelectionBandPlacementChecks.RunPhysical(Run,nativeOnly:true);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--selection-physical-payloads-only"}))
{
    SelectionBandPlacementChecks.RunPhysical(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--selection-band-placement-only"}))
{
    SelectionBandPlacementChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--editor-flood-components-only"}))
{
    EditorFloodComponentChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--properties-variable-metadata-only"}))
{
    Run("properties native variable deletion with understated payload metadata", DescriptorHeaderGuardChecks.MapVariableDeletion);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--descriptor-header-guards-only"}))
{
    DescriptorHeaderGuardChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--unknown-native-boundaries-only"}))
{
    HistoricalSectionInspectionChecks.NativeBoundaries(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--historical-section-inspection-only"}))
{
    HistoricalSectionInspectionChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--native-definition-unknown-stride-only"}))
{
    Run("packed inspector: unknown stride bounded opaque-frame representation", NativePackedInspectionChecks.UnknownStride);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--native-packed-inspection-only"}))
{
    NativePackedInspectionChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--native-script-partition-only"}))
{
    NativeScriptPartitionChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--compiled-owner-order-only"}))
{
    Run("compiled owner category order, stable list positions and explicit ambiguous binding refusal", CompiledOwnerOrderChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--scope-api-details-only"}))
{
    Run("scope compiler ownership diagnostic details", TestScriptScopes);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--native-collection-transaction-guards-only"}))
{
    NativeCollectionGuardChecks.Transactions(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--native-collection-guards-only"}))
{
    NativeCollectionGuardChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--native-collection-boundaries-only"}))
{
    NativeCollectionBoundaryChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--native-definition-official-tail-only"}))
{
    Run("native definition tails: official packing refuses opaque bytes instead of relocating or dropping them",NativeDefinitionTailChecks.OfficialPacking);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--native-definition-tails-only"}))
{
    NativeDefinitionTailChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--definition-future-copy-only"}))
{
    Run("definition layout guards: future inner versions remain opaque through actual raw Save/reopen",DefinitionLayoutGuardChecks.FutureVersions);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--definition-zero-stride-only"}))
{
    Run("definition layout guards: positive count with zero stride is refused before record allocation",DefinitionLayoutGuardChecks.ZeroStride);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--definition-layout-guards-only"}))
{
    DefinitionLayoutGuardChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--selection-owner-identity-only"}))
{
    SelectionOwnerIdentityChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--historical-scenario-counts-only"}))
{
    HistoricalScenarioCountChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--first-project-scenarios-only"}))
{
    Run("version-one scenario conversion and malformed upgrade rollback",TestFirstProjectScenarios);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--text-gate-identity-only"}))
{
    TextGateIdentityChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--native-missing-gate-conversion-only"}))
{
    NativeMissingGateConversionChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--compiler-reserved-names-only"}))
{
    CompilerReservedNameChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--compiler-action-bounds-only"}))
{
    CompilerActionBoundChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--compiler-placement-diagnostics-only"}))
{
    CompilerPlacementDiagnosticChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--compiler-thing-order-only"}))
{
    CompilerThingOrderChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--compiler-default-globals-only"}))
{
    CompilerDefaultGlobalChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--compiler-game-dates-only"}))
{
    CompilerGameDateChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--compiler-gate-fallback-only"}))
{
    CompilerGateFallbackChecks.Run(Run);
    Run("source project gate removal and reuse with compiler fallback",ProjectGateRemovalChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--definition-reference-diagnostics-only"}))
{
    DefinitionReferenceDiagnosticChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--player-import-policy-only"}))
{
    PlayerImportPolicyChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--player-layout-diagnostics-only"}))
{
    PlayerLayoutDiagnosticChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--document-reference-data-only"}))
{
    Run("live document metadata and unambiguous reference presentation",DocumentReferencePresentationChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--document-reference-presentation-only"}))
{
    Run("live document metadata and unambiguous reference presentation",DocumentReferencePresentationChecks.Run);
    Run("shared title reader keeps native/project title boundaries",WindowMapTitleChecks.Run);
    Run("changed named reference branches retain complete scenario page choices",ScenarioParameterDraftChecks.RunNamedPages);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--file-chooser-preview-only"}))
{
    Run("source file chooser detached preview and native dialog ABI/epochs",FileChooserPreviewChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--request-camera-images-only"}))
{
    Run("source request outcomes and original request/camera resources",RequestCameraImageChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--map-info-images-only"}))
{
    Run("source Info gold query and status/legend embedded resources",MapInfoImageChecks.Run);
    Run("shared terrain resolver retains map/player/band statistics",MapStatisticsChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--directional-control-images-only"}))
{
    Run("source directional/picker ICO resources and resize anchor display",DirectionalControlImageChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--direct-list-images-only"}))
{
    Run("source direct list icons and gate existing-site status metadata",DirectListImageChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--report-images-only"}))
{
    Run("source report severity ICO identities and compiled resources",ReportImageChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--map-ui-captions-only"}))
{
    Run("source map UI captions and creature appearance override context",MapUICaptionChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--spatial-custom-captions-only"}))
{
    Run("source spatial custom captions, effect scale direction and patrol units",SpatialCustomCaptionChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--placed-thing-captions-only"}))
{
    Run("source placed-thing data union and contextual raw record captions",PlacedThingCaptionChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--definition-caption-routes-only"}))
{
    Run("source contextual definition captions across table and raw record routes",DefinitionCaptionRouteChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--hero-band-display-only"}))
{
    Run("source hero-band selector, elite names and complete member flags",HeroBandDisplayChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--reward-destination-images-only"}))
{
    Run("source font-relative rewards and scenario destination images",RewardDestinationImageChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--map-palette-images-only"}))
{
    Run("source toolbar palette images, visible rows and tool identities",MapPaletteImageChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--scenario-point-band-previews-only"}))
{
    Run("source Action Point band headers, member details and physical selection identity",ScenarioPointBandPreviewChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--scenario-point-owner-images-only"}))
{
    Run("source scenario point owner images and original category fallbacks",ScenarioPointOwnerImageChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--map-rule-fill-images-only"}))
{
    Run("source rule matrix and packed new-map fill images with read-only data",MapRuleFillImageChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--editor-list-images-only"}))
{
    Run("source patrol/scenario/manager list image identities and warning precedence",EditorListImageChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--player-ai-room-images-only"}))
{
    Run("source AI room priority image identities, compiled BMP bytes and read-only order",PlayerAIRoomImageChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--player-ai-field-captions-only"}))
{
    Run("contextual raw player AI captions, historical layouts and read-only state",PlayerAIFieldCaptionChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--scenario-band-previews-only"}))
{
    Run("source scenario band member previews and immutable read-only snapshots",ScenarioBandPreviewChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--scenario-catalogue-images-only"}))
{
    Run("source scenario catalogue image identities and overlapping categories",ScenarioCatalogueImageChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--creature-spell-usage-only"}))
{
    Run("source creature spell used-by list and read-only private snapshot",CreatureSpellUsageChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--creature-resistance-slider-only"}))
{
    Run("source creature resistance slider positions and private explicit scroll",CreatureResistanceSliderChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--projectile-child-input-only"}))
{
    Run("source projectile and trap trigger numeric callbacks with approved corrections",ProjectileChildInputChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--global-element-input-only"}))
{
    Run("source global element numeric callbacks across five private pages",GlobalElementInputChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--creature-job-input-only"}))
{
    Run("source creature job numeric input and spin before private Add Change",CreatureJobInputChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--creature-auxiliary-input-only"}))
{
    Run("source possession and creature spell numeric callbacks with private common Apply",CreatureAuxiliaryInputChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--creature-attack-input-only"}))
{
    Run("source creature attack numeric callbacks and private common Apply",CreatureAttackInputChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--creature-attribute-input-only"}))
{
    Run("source creature attribute callbacks and native width guards with private Apply",CreatureAttributeInputChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--creature-number-inputs-only"}))
{
    Run("source creature light input clamps, fractional preservation and shared private Apply",CreatureLightInputChecks.Run);
    Run("source creature parameter amount input and unsigned spin with private Apply",CreatureParameterInputChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--creature-light-input-only"}))
{
    Run("source creature light input clamps, fractional preservation and shared private Apply",CreatureLightInputChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--creature-catalogue-export-only"}))
{
    Run("private creature percentage catalogue projection and guarded atomic copy export",()=>CreatureLevelCatalogueExportChecks.Run(FindRepoRoot()));
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--creature-level-defaults-only"}))
{
    Run("shared creature level defaults, typed missing overrides and common parent Apply with seven files",()=>CreatureLevelModifierDefaultsChecks.Run(FindRepoRoot()));
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--creature-level-session-only"}))
{
    Run("existing creature level native and DKM private session, immutable source delta and seven-file compilation",()=>CreatureLevelModifierSessionChecks.Run(FindRepoRoot()));
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--creature-level-writer-only"}))
{
    Run("official signed creature level writer, zero sentinel and exact existing-collection patch",()=>CreatureLevelModifierWriterChecks.Run(FindRepoRoot()));
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--creature-level-draft-only"}))
{
    Run("official creature level statistic controls, signed ranges and private draft page transitions",CreatureLevelModifierDraftChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--creature-level-read-only"}))
{
    Run("official creature level modifier tuples, bounded read-only sources and DKM retention",()=>CreatureLevelModifierReadChecks.Run(FindRepoRoot()));
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--manager-destination-only"}))
{
    Run("source manager destination numeric session, bounded shared Add and independent preferences",ManagerDestinationChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--scenario-new-point-only"}))
{
    Run("source distinct New Point draft, additional player points and occupied-owner boundaries",ScenarioNewPointChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--region-coordinate-only"}))
{
    Run("source region coordinate input clamp, one-unit steps and private atomic draft boundaries",RegionCoordinateChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--scenario-expansion-only"}))
{
    Run("source stored condition expansion, noncommand history, native-to-DKM view projection and exact seven-file export",()=>ScenarioExpansionChecks.Run(FindRepoRoot()));
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--scenario-insertion-only"}))
{
    Run("source scenario root, child and sibling append destinations, exact incoming insertion and shared occurrence guards",ScenarioInsertionChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--region-creation-only"}))
{
    Run("source full region creation draft, repeated Add, atomic generation and guarded history",RegionCreationChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--scenario-retained-type-only"}))
{
    Run("source retained scenario category type, wizard closure and read-only owner lifetime",ScenarioRetainedTypeChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--special-keyboard-only"}))
{
    Run("source special keyboard routing, cell-aligned scrolling and quick-close template masks",SpecialKeyboardChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--source-keyboard-only"}))
{
    Run("source nonconflicting keyboard chords, drawing modes and explicit placement families",SourceKeyboardChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--help-section-versions-only"}))
{
    Run("source section versions, bounded input headers, generated sections and packaged bilingual help paths",HelpSectionVersionsChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--history-clear-only"}))
{
    Run("source clear history retains depth, live bytes, counters and future Undo with active-edit guards",HistoryClearChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--manager-list-rules-only"}))
{
    Run("source manager list scope, imported coordinate boundaries and unique family membership",ManagerListRulesChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--disk-progress-only"}))
{
    Run("source disk progress, observer-neutral loads and unchanged atomic writer failures",()=>DiskProgressChecks.Run(FindRepoRoot()));
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--compilation-snapshot-only"}))
{
    Run("independent compilation snapshots, live editing isolation and exact official seven-file output",()=>CompilationSnapshotChecks.Run(FindRepoRoot()));
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--file-dialog-output-only"}))
{
    Run("source file dialog scopes and confirmed thumbnail output failure atomicity",FileDialogOutputChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--document-report-rules-only"}))
{
    Run("source report compatibility filters, continuation and bounded navigation",DocumentReportRulesChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--window-map-title-only"}))
{
    Run("source default window map title and bounded historical metadata reads",WindowMapTitleChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--editor-sounds-only"}))
{
    Run("source editor sound routing, original samples and centibel volume",EditorSoundRulesChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--manager-flow-rules-only"}))
{
    Run("source object manager option and preview routing decisions",ManagerFlowRulesChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--object-manager-history-only"}))
{
    Run("source object manager local Undo ownership, command boundaries and committed failure lifetime",ObjectManagerHistoryChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--history-memory-notice-only"}))
{
    Run("source informational Undo allocation notice, post-rollback lifetime, session preference and nested lease",HistoryMemoryNoticeChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--gate-generation-requests-only"}))
{
    Run("source gate generation requests, independent capacity notices, shared approval and guarded parent lifetimes",GateGenerationRequestChecks.Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--copy-confirmation-state-only"}))
{
    Run("copy-buffer remembered answer is session-only while the explicit menu preference survives restart",CopyConfirmationStateChecks.Run);
    return failures.Count==0?0:1;
}
if(args.Length==2&&args[0]=="--loaded-object-manager-only")
{
    Run("loaded source manager removal with literal derived crystal WORDs and full Undo",()=>ObjectManagerChecks.CheckLoaded(NativeMapDocument.Load(args[1])));
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--scenario-terrain-owners-only"}))
{
    Run("55 named contexts and 28 condition pages, source identities, paired interface, exact bytes and history",ScenarioParameterDraftChecks.RunNamedPages);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--definition-images-only"}))
{
    Run("source global definition defaults, editing and section-scoped resets",GlobalDefinitionChecks.Run);
    return failures.Count==0?0:1;
}
if(args.Length==2&&args[0]=="--write-global-control-inventory")
{
    GlobalControlInventory.Write(args[1]);return 0;
}
if (args.SequenceEqual(new[] { "--patrol-pause-only" }))
{
    Run("source patrol numeric pause draft, decimal prefix, range retry, cancel and one guarded acceptance",EditorPatrolPauseChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--patrol-removal-only" }))
{
    Run("source patrol removal requests, default Yes/No, separate session flags and one guarded acceptance",EditorPatrolRemovalChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--patrol-warning-only" }))
{
    Run("source patrol warning requests, selection/all predicates, session flags and guarded read-only replies",EditorPatrolWarningChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--patrol-selection-only" }))
{
    Run("source patrol area selection, independent candidates, pause drafts, cancel and guarded acceptance",()=>EditorPatrolSelectionChecks.Run(FindRepoRoot()));
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--patrol-manager-only" }))
{
    Run("source patrol manager local occurrences, indirect warnings, guarded lifetime and atomic fields",EditorPatrolManagerChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--patrol-grouping-only" }))
{
    Run("source patrol tree grouping, exhaustive file-order graphs and independent packed Level10",()=>EditorPatrolGroupingChecks.Run(FindRepoRoot()));
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--editor-patrol-only" }))
{
    Run("source patrol editor acceptance counters, exact field-only undo and generic no-op retention",EditorPatrolCallerChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--region-generation-only" }))
{
    Run("private nested gate generation, source counters, full topology replay and parent cancel/apply",RegionGateGenerationChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--gate-generation-only" }))
{
    Run("original hero-gate generator, templates, flags and atomic scenario additions",HeroGateGenerationChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--region-picking-only" }))
{
    Run("source point/rectangle private region choice, cancellation, lifecycle and deferred atomic apply",RegionGeometryPickChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--region-properties-only" }))
{
    Run("private source region properties, nested options, equal acceptance and exact atomic history",RegionPropertiesSessionChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--selection-order-only" }))
{
    Run("source selection object order, stable stacked creatures and transformed buffer retention",SelectionObjectOrderChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--selection-terrain-only" }))
{
    Run("source-compatible KMAP projection and exact raw selection terrain retention",SelectionTerrainProjectionChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--historical-selection-only" }))
{
    Run("historical selection original-reader defaults and exact native/project projections",HistoricalSelectionChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--entity-diagnostics-only" }))
{
    Run("read-only missing and ambiguous entity references, namespace/sentinel/active-slot guards",EntityReferenceDiagnosticChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
// Candidate regression must inspect its own localized resources. Other source oracles still locate preserved original Source/Binaries.
var repoRoot = Environment.GetEnvironmentVariable("DK2_VALIDATION_RESOURCE_ROOT") is { Length: > 0 } resourceRoot
    ? Path.GetFullPath(resourceRoot) : FindRepoRoot();
if (args.SequenceEqual(new[] { "--selection-files-only" }))
{
    Run("DKZ compatibility, script retention and safe file import/export",TestSelectionFiles);
    Run("selection protection masks, original paste/transform policy and DKCP v3",TestSelectionProtection);
    Run("historical selection original-reader defaults and exact native/project projections",HistoricalSelectionChecks.Run);
    Run("source-compatible KMAP projection and exact raw selection terrain retention",SelectionTerrainProjectionChecks.Run);
    Run("source selection object order, stable stacked creatures and transformed buffer retention",SelectionObjectOrderChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--band-api-only" }))
{
    Run("sixteen-slot band editing, scope retention and all project versions", TestBandMembers);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--scope-api-only" }))
{
    Run("all nine owner scopes, binding lifecycle and atomic ownership edits", TestScriptScopes);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--script-identities-only" }))
{
    Run("typed entity references, namespaces and atomic identity edits", TestEntityIdentities);
    Run("read-only scenario scope, cycle and historical compatibility diagnostics", TestScriptDiagnostics);
    Run("all original event argument layouts", TestEvents);
    return failures.Count == 0 ? 0 : 1;
}
if(args.Length>=2&&args[0]=="--real-text-projection-only")
{
    var inputs=args[1..];foreach(var input in inputs)ValidateExternalPath(input);
    if(reportPath is not null)
    {
        using var report=new FileStream(reportPath,FileMode.CreateNew,FileAccess.Write,FileShare.None);
        System.Text.Json.JsonSerializer.Serialize(report,new {TimestampUtc=DateTime.UtcNow,Inputs=inputs,Passed=passedChecks,Failures=failures},new System.Text.Json.JsonSerializerOptions {WriteIndented=true});
    }
    Console.WriteLine(failures.Count==0?"REAL TEXT PROJECTION FOCUS PASSED: full gates and original GUI remain required":"REAL TEXT PROJECTION FOCUS FAILED");
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--activity-save-continuation-only"}))
{
    ActivitySaveContinuationChecks.Run(Run);
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--save-projection-oracle-only"}))
{
    SaveProjectionOracleChecks.Run(Run);
    Console.WriteLine(failures.Count==0?"SAVE PROJECTION ORACLE7 PASSED: malformed output refusal verified":"SAVE PROJECTION ORACLE7 FAILED");
    return failures.Count==0?0:1;
}
if(args.SequenceEqual(new[]{"--save-projection-regression-only"}))
{
    Run("parent map-properties composition, cancel, consent, nine anchors, stale scope and exact undo",MapPropertiesSessionChecks.Run);
    Run("general map name, keeper count, native working target, historical guards and exact atomic history",MapGeneralSettingsChecks.Run);
    Run("original speech/text metadata, ANSI bank bytes, thirty fields, historical drafts and atomic undo",MapSpeechTextChecks.Run);
    Run("source level scalars, all flag combinations, rewards, historical fields and atomic undo",MapLevelOptionsChecks.Run);
    Run("original scenario aliases, operand counts, native editor metadata and exact history",ScenarioNameChecks.Run);
    Run("all original camera paths, source cell-mode inversion, atomic action editing and exact round trips",CameraPathChecks.Run);
    Run("legacy editor ABI and editable project round-trip",TestLegacyProjectEditing);
    Run("text MAP sections, scripts, defaults and atomic rejection",TestTextMaps);
    Run("original no-scenario MAP policy, malformed graphs, all scopes and exact complete snapshots",OriginalTextExportChecks.Run);
    Run("historical project section upgrades and atomic undo",TestProjectUpgrades);
    Run("original bidirectional selection rotation, reflections and identity retention",TestSelectionTransforms);
    Run("original new-map presets, seven player defaults, versions and starter layouts",TestNewMapCreation);
    Run("original area/gate/effect geometry, category IDs and atomic map interaction",TestMapRegions);
    Run("original hero-gate generator, templates, flags and atomic scenario additions",HeroGateGenerationChecks.Run);
    Run("original availability/pool/modifier/alliance matrices and exact batch undo",MapRuleChecks.Run);
    Run("original named global variables, ranges, 20.12 display and exact batch reset",MapVariableChecks.Run);
    Run("all source manual AI settings, named options, room order, stale guards and exact undo",PlayerAIOptionsChecks.Run);
    if(reportPath is not null)
    {
        using var report=new FileStream(reportPath,FileMode.CreateNew,FileAccess.Write,FileShare.None);
        System.Text.Json.JsonSerializer.Serialize(report,new {TimestampUtc=DateTime.UtcNow,Inputs=Array.Empty<string>(),Passed=passedChecks,Failures=failures},new System.Text.Json.JsonSerializerOptions {WriteIndented=true});
    }
    Console.WriteLine(failures.Count==0?"SAVE PROJECTION REGRESSION17 PASSED: all full gates/original GUI/delivery remain required":"SAVE PROJECTION REGRESSION17 FAILED");
    return failures.Count==0?0:1;
}
if (args.SequenceEqual(new[] { "--creature-pages-only" }))
{
    Run("original four remaining creature pages, all75 fields/45 rows, typed source rules, literal bytes and shared atomic context", CreaturePageChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--creature-jobs-warning-only" }))
{
    Run("source creature job warning Continue/Cancel, signed LONG totals and read-only session auto reply", CreatureJobsWarningChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--creature-jobs-page-context-only" }))
{
    Run("source retained creature Jobs page buffer, current-row callbacks and guarded common Apply", CreatureJobsPageContextChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--creature-spell-only" }))
{
    Run("original complete creature spell page/private projectile, all41 rows/source ranges/literal bytes and shared atomic context", CreatureSpellChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--creature-globals-session-only" }))
{
    Run("original nine-page creature shared context, private composition, job warning and one atomic Undo", CreatureGlobalsSessionChecks.Composition);
    Run("original nine-section current/all private resets over7471 values and lifetime/scope guards", CreatureGlobalsSessionChecks.ResetsAndGuards);
    Run("shared creature inactive flags/unused slot, raw fingerprints and real44/46-row5449/5537 variants", CreatureGlobalsSessionChecks.OpaqueAndVariants);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--global-variables-session-only" }))
{
    Run("source shared five-page context, 98 rows/two private spell slots, cancel, no-op and one guarded Undo", GlobalVariablesSessionChecks.Composition);
    Run("source 2016 shared reset fields, current/all private resets, opaque scope and lifetime guards", GlobalVariablesSessionChecks.ResetsAndScope);
    Run("shared global context preserves unused room ceiling/unedited arrays and rejects raw fingerprint changes", GlobalVariablesSessionChecks.OpaqueGuards);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--global-spell-only" }))
{
    Run("original complete keeper spell parent/two children, source catalogues/ranges, integer upgrade recharge and atomic guards", GlobalSpellChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--global-trap-only" }))
{
    Run("original complete trap parent/children, source masks/ranges/catalogues, hidden retention and atomic guards", GlobalTrapChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--global-door-only" }))
{
    Run("original complete door controls, source catalogues/ranges, disabled mana buddy/no input and atomic guards", GlobalDoorChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--global-room-only" }))
{
    Run("original complete room controls, CantBuild/BattleEffect/masks/ranges/RGB and atomic guards", GlobalRoomChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--global-terrain-only" }))
{
    Run("original complete terrain controls, source transformations/ranges/RGB Save and atomic guards", GlobalTerrainChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--scenario-existing-wizard-only" }))
{
    Run("original existing-event private wizard, 103 imported types/26 Save cleanup, retained identities/links and atomic guards", ScenarioParameterDraftChecks.RunExistingWizard);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--scenario-speech-only" }))
{
    Run("original speech controls, source four-digit IDs/checkbox, literal bytes, atomic history, compiler and guards", ScenarioParameterDraftChecks.RunSpeechPage);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--scenario-spatial-only" }))
{
    Run("original spatial point/area offsets, catalogues, byte preservation, atomic history and compiler", ScenarioParameterDraftChecks.RunSpatialPoints);
    Run("original gate IDs/native coordinates, ambiguity and lifetime guards", ScenarioParameterDraftChecks.RunSpatialGates);
    Run("original rectangle creation order, tail, private lifetime and atomic history", ScenarioParameterDraftChecks.RunSpatialRectangles);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--scenario-creation-session-only" }))
{
    Run("original private creation, repeated canceled temporary values, atomic alias/repeat/insertion, 103 types and guards", ScenarioParameterDraftChecks.RunCreationSession);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--scenario-editor-save-only" }))
{
    Run("source explicit parameter Save, 20 zero branches/75 retained pages, raw preservation and exact atomic history", ScenarioParameterDraftChecks.RunEditorParameterSave);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--scenario-creation-defaults-only" }))
{
    Run("103 original creation/type-change defaults, 26 save-time zero types, same-type retention and exact atomic history", ScenarioParameterDraftChecks.RunCreationDefaults);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--scenario-spawn-parent-only" }))
{
    Run("creature-spawn parent, source choices/7 flags, private point/area composition, exact bytes and atomic history", ScenarioParameterDraftChecks.RunSpawnParent);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--scenario-camera-parent-only" }))
{
    Run("camera parent unsigned bounds, all 52 source profiles/ceilings, private composition, literal bytes and atomic history", ScenarioParameterDraftChecks.RunCameraParent);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--scenario-parameters-only" }))
{
    Run("69 parameter/26 repeat-only drafts, all 30 comparison pages, literal bytes, atomic history and guards", ScenarioParameterDraftChecks.Run);
    Run("19 original scalar pages and 26 repeat pages, sparse range validation and atomic control batches", ScenarioParameterDraftChecks.RunControlPages);
    Run("55 named contexts and 28 condition pages, source identities, paired interface, exact bytes and history", ScenarioParameterDraftChecks.RunNamedPages);
    Run("12 original action pages, source catalogues, map identities, prerequisites, exact bytes and atomic history", ScenarioParameterDraftChecks.RunActionPages);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--scenario-alias-events-only" }))
{
    Run("source inline alias event operands, atomic names, unknown guards and exact history", ScenarioAliasEventChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--native-creature-variants-only" }))
{
    Run("real 44/46-row creature 5449/5537 prefixes, every field/reset, opaque tails and malformed guards", NativeCreatureVariantChecks.Run);
    Run("source global definition defaults, editing and section-scoped resets", GlobalDefinitionChecks.Run);
    Run("source object batch placement, verified native flags and unsupported layout guards", ObjectBatchPlacementChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--map-properties-only" }))
{
    Run("parent map-properties composition, cancel, consent, nine anchors, stale scope and exact undo", MapPropertiesSessionChecks.Run);
    Run("all original resize backgrounds, native working retention and reload defaults", MapPropertiesSessionChecks.FillState);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--map-general-only" }))
{
    Run("general map name, keeper count, native working target, historical guards and exact atomic history", MapGeneralSettingsChecks.Run);
    Run("source game-version notices, independent operand mapping, non-failing compile and native reload", GameVersionCompatibilityChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--speech-text-only" }))
{
    Run("original speech/text metadata, ANSI bank bytes, thirty fields, historical drafts and atomic undo", MapSpeechTextChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--game-resources-only" }))
{
    Run("bounded original STR/SDT resources, text/speech catalogues and immutable installed inputs", GameResourceChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--level-options-only" }))
{
    Run("source level scalars, all flag combinations, rewards, historical fields and atomic undo", MapLevelOptionsChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--scenario-names-only" }))
{
    Run("original scenario aliases, operand counts, native editor metadata and exact history", ScenarioNameChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--scenario-descriptions-only" }))
{
    Run("all 103 original event descriptions, literal operands, EN/RU grammar, aliases, repeats and read-only projection", ScenarioDescriptionChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--unused-scripts-only" }))
{
    Run("source unused-event roots, retained validation, shared graphs, atomic removal and monotonic consent", UnusedScriptChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--map-overlays-only" }))
{
    Run("source camera overlap, region metadata, ordered gate edges, integer paths and read-only overlays", MapOverlayChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--terrain-zoom-only" }))
{
    Run("source terrain bitmap/owner-frame identities, all zoom presets and independent dimensions", MapTerrainZoomChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--map-object-display-only" }))
{
    Run("source map object geometry, view flags, owner colours and read-only snapshots", MapObjectDisplayChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--object-painting-only" }))
{
    Run("single source object painting, door toggles, all opposites, stable identities and exact undo", ObjectPaintingChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--object-modification-only" }))
{
    Run("source masked bulk modification, relative group translation, collisions, scopes and exact undo", ObjectModificationChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--object-manager-only" }))
{
    Run("source manager display sorting, selected bulk removal, scope preservation and exact undo", ObjectManagerChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--object-insertion-only" }))
{
    Run("full source templates, batch insertion/replacement, scopes and atomic round trips", ObjectBatchInsertionChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--object-placement-only" }))
{
    Run("source object batch placement, terrain flags, replacement previews and read-only stale guards", ObjectBatchPlacementChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--random-paint-only" }))
{
    Run("original GDI masks, Shift random sampling, source probability/order, special footprints and exact undo", RandomPaintChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--history-only" }))
{
    Run("original undo depth, immutable nested history, exact transactions, trimming and snapshot budget", EditHistoryChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--legacy-compiler-only" }))
{
    Run("legacy editor ABI and editable project round-trip", TestLegacyProjectEditing);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--camera-paths-only" }))
{
    Run("all original camera paths, source cell-mode inversion, atomic action editing and exact round trips", CameraPathChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--inspector-references-only" }))
{
    Run("actual KWD-linked companions, shared definitions, CP1252 paths, fallback precedence and malformed tables", InspectorReferenceChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--thumbnail-only" }))
{
    Run("original thumbnail flags, colour-key/background, both BMP sizes and read-only atomic export",MapThumbnailChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--fullmap-only" }))
{
    Run("source fullscreen animation, neutral tiles, original icon identities and read-only entities",FullMapOverviewChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--original-text-only" }))
{
    Run("original no-scenario MAP policy, malformed graphs, all scopes and exact complete snapshots",OriginalTextExportChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--overview-only" }))
{
    Run("source minimap colours, native conversion fallback, read-only BGRA and saved overview",MapOverviewChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--player-ai-only" }))
{
    Run("all source manual AI settings, named options, room order, stale guards and exact undo",PlayerAIOptionsChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--native-sizes-only" }))
{
    Run("source native file-size metadata warnings, actual bounds and exact opaque-byte retention",NativeFileSizeChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--native-tiles-only" }))
{
    Run("original native tile bounds, ignored size metadata, opaque tails and exact resize undo",NativeTileLayoutChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--native-references-only" }))
{
    Run("explicit KWD references over stale local fallback companions, exact export and reopening",NativeReferenceChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--effect-generators-only" }))
{
    Run("source effect-generator four slots, frequency, named choices and atomic undo", EffectGeneratorChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--decorations-only" }))
{
    Run("source named effects, room slot masks, room objects and armour with exact atomic undo", GlobalDecorationChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--definition-rules-only" }))
{
    Run("source door/trap/keeper-spell named rules, trigger parameters and atomic undo", GlobalRuleChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--creature-options-only" }))
{
    Run("source creature resistance/possession catalogues, whole-group drafts and exact undo", CreatureOptionChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

if (args.SequenceEqual(new[] { "--definition-numbers-only" }))
{
    Run("source definition units, signed attributes, exact fractional display and raw preservation", GlobalDefinitionNumericChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--projectiles-only" }))
{
    Run("source projectile forms, targets, signed parameters, inactive compiler fields and exact undo", GlobalProjectileChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--jobs-only" }))
{
    Run("source creature jobs, four groups, warning override, signed tuples and exact undo", CreatureJobChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}
if (args.SequenceEqual(new[] { "--attacks-only" }))
{
    Run("source creature attack slots, named spells/melee, limits, inactive values and exact undo", CreatureAttackChecks.Run);
    return failures.Count == 0 ? 0 : 1;
}

Run("synthetic KWD metadata", TestSyntheticDescriptor);
Run("source creature light input clamps, fractional preservation and shared private Apply",CreatureLightInputChecks.Run);
Run("source creature parameter amount input and unsigned spin with private Apply",CreatureParameterInputChecks.Run);
Run("source creature attribute callbacks and native width guards with private Apply",CreatureAttributeInputChecks.Run);
Run("source creature attack numeric callbacks and private common Apply",CreatureAttackInputChecks.Run);
Run("source possession and creature spell numeric callbacks with private common Apply",CreatureAuxiliaryInputChecks.Run);
Run("source creature job numeric input and spin before private Add Change",CreatureJobInputChecks.Run);
Run("source global element numeric callbacks across five private pages",GlobalElementInputChecks.Run);
Run("source projectile and trap trigger numeric callbacks with approved corrections",ProjectileChildInputChecks.Run);
Run("source creature resistance slider positions and private explicit scroll",CreatureResistanceSliderChecks.Run);
Run("source creature spell used-by list and read-only private snapshot",CreatureSpellUsageChecks.Run);
Run("source scenario catalogue image identities and overlapping categories",ScenarioCatalogueImageChecks.Run);
Run("source scenario band member previews and immutable read-only snapshots",ScenarioBandPreviewChecks.Run);
Run("source patrol/scenario/manager list image identities and warning precedence",EditorListImageChecks.Run);
Run("source rule matrix and packed new-map fill images with read-only data",MapRuleFillImageChecks.Run);
Run("source scenario point owner images and original category fallbacks",ScenarioPointOwnerImageChecks.Run);
Run("source Action Point band headers, member details and physical selection identity",ScenarioPointBandPreviewChecks.Run);
Run("source toolbar palette images, visible rows and tool identities",MapPaletteImageChecks.Run);
Run("private creature percentage catalogue projection and guarded atomic copy export",()=>CreatureLevelCatalogueExportChecks.Run(FindRepoRoot()));
Run("shared creature level defaults, typed missing overrides and common parent Apply with seven files",()=>CreatureLevelModifierDefaultsChecks.Run(FindRepoRoot()));
Run("existing creature level native and DKM private session, immutable source delta and seven-file compilation",()=>CreatureLevelModifierSessionChecks.Run(FindRepoRoot()));
Run("official signed creature level writer, zero sentinel and exact existing-collection patch",()=>CreatureLevelModifierWriterChecks.Run(FindRepoRoot()));
Run("official creature level statistic controls, signed ranges and private draft page transitions",CreatureLevelModifierDraftChecks.Run);
Run("official creature level modifier tuples, bounded read-only sources and DKM retention",()=>CreatureLevelModifierReadChecks.Run(FindRepoRoot()));
Run("source manager destination numeric session, bounded shared Add and independent preferences",ManagerDestinationChecks.Run);
Run("source distinct New Point draft, additional player points and occupied-owner boundaries",ScenarioNewPointChecks.Run);
Run("source region coordinate input clamp, one-unit steps and private atomic draft boundaries",RegionCoordinateChecks.Run);
Run("source stored condition expansion, noncommand history, native-to-DKM view projection and exact seven-file export",()=>ScenarioExpansionChecks.Run(FindRepoRoot()));
Run("source scenario root, child and sibling append destinations, exact incoming insertion and shared occurrence guards",ScenarioInsertionChecks.Run);
Run("source retained scenario category type, wizard closure and read-only owner lifetime",ScenarioRetainedTypeChecks.Run);
Run("source special keyboard routing, cell-aligned scrolling and quick-close template masks",SpecialKeyboardChecks.Run);
Run("source nonconflicting keyboard chords, drawing modes and explicit placement families",SourceKeyboardChecks.Run);
Run("source section versions, bounded input headers, generated sections and packaged bilingual help paths",HelpSectionVersionsChecks.Run);
Run("source object manager option and preview routing decisions",ManagerFlowRulesChecks.Run);
Run("source editor sound routing, original samples and centibel volume",EditorSoundRulesChecks.Run);
Run("source default window map title and bounded historical metadata reads",WindowMapTitleChecks.Run);
Run("source report compatibility filters, continuation and bounded navigation",DocumentReportRulesChecks.Run);
Run("source file dialog scopes and confirmed thumbnail output failure atomicity",FileDialogOutputChecks.Run);
Run("independent compilation snapshots, live editing isolation and exact official seven-file output",()=>CompilationSnapshotChecks.Run(FindRepoRoot()));
Run("source disk progress, observer-neutral loads and unchanged atomic writer failures",()=>DiskProgressChecks.Run(FindRepoRoot()));
Run("source manager list scope, imported coordinate boundaries and unique family membership",ManagerListRulesChecks.Run);
Run("source clear history retains depth, live bytes, counters and future Undo with active-edit guards",HistoryClearChecks.Run);
Run("source object manager local Undo ownership, command boundaries and committed failure lifetime",ObjectManagerHistoryChecks.Run);
Run("source informational Undo allocation notice, post-rollback lifetime, session preference and nested lease",HistoryMemoryNoticeChecks.Run);
Run("source gate generation requests, independent capacity notices, shared approval and guarded parent lifetimes",GateGenerationRequestChecks.Run);
Run("copy-buffer remembered answer is session-only while the explicit menu preference survives restart",CopyConfirmationStateChecks.Run);
Run("parent map-properties composition, cancel, consent, nine anchors, stale scope and exact undo", MapPropertiesSessionChecks.Run);
Run("all original resize backgrounds, native working retention and reload defaults", MapPropertiesSessionChecks.FillState);
Run("general map name, keeper count, native working target, historical guards and exact atomic history", MapGeneralSettingsChecks.Run);
Run("source game-version notices, independent operand mapping, non-failing compile and native reload", GameVersionCompatibilityChecks.Run);
Run("original speech/text metadata, ANSI bank bytes, thirty fields, historical drafts and atomic undo", MapSpeechTextChecks.Run);
Run("bounded original STR/SDT resources, text/speech catalogues and immutable installed inputs", GameResourceChecks.Run);
Run("source level scalars, all flag combinations, rewards, historical fields and atomic undo", MapLevelOptionsChecks.Run);
Run("original scenario aliases, operand counts, native editor metadata and exact history", ScenarioNameChecks.Run);
Run("source inline alias event operands, atomic names, unknown guards and exact history", ScenarioAliasEventChecks.Run);
Run("camera parent unsigned bounds, all 52 source profiles/ceilings, private composition, literal bytes and atomic history", ScenarioParameterDraftChecks.RunCameraParent);
Run("creature-spawn parent, source choices/7 flags, private point/area composition, exact bytes and atomic history", ScenarioParameterDraftChecks.RunSpawnParent);
Run("103 original creation/type-change defaults, 26 save-time zero types, same-type retention and exact atomic history", ScenarioParameterDraftChecks.RunCreationDefaults);
Run("source explicit parameter Save, 20 zero branches/75 retained pages, raw preservation and exact atomic history", ScenarioParameterDraftChecks.RunEditorParameterSave);
Run("original speech controls, source four-digit IDs/checkbox, literal bytes, atomic history, compiler and guards", ScenarioParameterDraftChecks.RunSpeechPage);
Run("original existing-event private wizard, 103 imported types/26 Save cleanup, retained identities/links and atomic guards", ScenarioParameterDraftChecks.RunExistingWizard);
Run("original spatial point/area offsets, catalogues, byte preservation, atomic history and compiler", ScenarioParameterDraftChecks.RunSpatialPoints);
Run("original gate IDs/native coordinates, ambiguity and lifetime guards", ScenarioParameterDraftChecks.RunSpatialGates);
Run("original rectangle creation order, tail, private lifetime and atomic history", ScenarioParameterDraftChecks.RunSpatialRectangles);
Run("original private creation, repeated canceled temporary values, atomic alias/repeat/insertion, 103 types and guards", ScenarioParameterDraftChecks.RunCreationSession);
Run("all 103 original event descriptions, literal operands, EN/RU grammar, aliases, repeats and read-only projection", ScenarioDescriptionChecks.Run);
Run("69 parameter/26 repeat-only drafts, all 30 comparison pages, literal bytes, atomic history and guards", ScenarioParameterDraftChecks.Run);
Run("19 original scalar pages and 26 repeat pages, sparse range validation and atomic control batches", ScenarioParameterDraftChecks.RunControlPages);
Run("55 named contexts and 28 condition pages, source identities, paired interface, exact bytes and history", ScenarioParameterDraftChecks.RunNamedPages);
Run("12 original action pages, source catalogues, map identities, prerequisites, exact bytes and atomic history", ScenarioParameterDraftChecks.RunActionPages);
Run("source unused-event roots, retained validation, shared graphs, atomic removal and monotonic consent", UnusedScriptChecks.Run);
Run("source camera overlap, region metadata, ordered gate edges, integer paths and read-only overlays", MapOverlayChecks.Run);
Run("source terrain bitmap/owner-frame identities, all zoom presets and independent dimensions", MapTerrainZoomChecks.Run);
Run("source map object geometry, view flags, owner colours and read-only snapshots", MapObjectDisplayChecks.Run);
Run("single source object painting, door toggles, all opposites, stable identities and exact undo", ObjectPaintingChecks.Run);
Run("source masked bulk modification, relative group translation, collisions, scopes and exact undo", ObjectModificationChecks.Run);
Run("full source templates, batch insertion/replacement, scopes and atomic round trips", ObjectBatchInsertionChecks.Run);
Run("source manager display sorting, selected bulk removal, scope preservation and exact undo", ObjectManagerChecks.Run);
Run("source object batch placement, terrain flags, replacement previews and read-only stale guards", ObjectBatchPlacementChecks.Run);
Run("original GDI masks, Shift random sampling, source probability/order, special footprints and exact undo", RandomPaintChecks.Run);
Run("original undo depth, immutable nested history, exact transactions, trimming and snapshot budget", EditHistoryChecks.Run);
Run("all original camera paths, source cell-mode inversion, atomic action editing and exact round trips", CameraPathChecks.Run);
Run("actual KWD-linked companions, shared definitions, CP1252 paths, fallback precedence and malformed tables", InspectorReferenceChecks.Run);
Run("synthetic Map.kld dimensions", TestSyntheticMapTiles);
Run("legacy DKM header", TestLegacyEditorMap);
Run("corrupt size detection", TestCorruptSize);
Run("English/Russian key parity", TestLocalizationParity);
Run("legacy native schema offsets", TestSchemas);
Run("drawing and clipboard transforms", TestDrawing);
Run("source minimap colours, native conversion fallback, read-only BGRA and saved overview",MapOverviewChecks.Run);
Run("source fullscreen animation, neutral tiles, original icon identities and read-only entities",FullMapOverviewChecks.Run);
Run("original thumbnail flags, colour-key/background, both BMP sizes and read-only atomic export",MapThumbnailChecks.Run);
Run("native tile editing, undo and lossless export", TestNativeEditing);
Run("explicit KWD references over stale local fallback companions, exact export and reopening",NativeReferenceChecks.Run);
Run("original native tile bounds, ignored size metadata, opaque tails and exact resize undo",NativeTileLayoutChecks.Run);
Run("source native file-size metadata warnings, actual bounds and exact opaque-byte retention",NativeFileSizeChecks.Run);
Run("malformed dimensions are diagnostics, not exceptions", TestMalformedDimensions);
Run("new maps and all native record templates", TestRecordEditing);
Run("global compiler-derived schemas and bit flags", TestGlobalSchemas);
Run("all original event argument layouts", TestEvents);
Run("independent script graph duplication", TestScriptCloning);
Run("legacy section bounds and unknown-byte preservation", TestLegacySections);
Run("map resizing with objects, scripts and atomic undo", TestResize);
Run("legacy editor ABI and editable project round-trip", TestLegacyProjectEditing);
Run("entity-aware selection clipboard and transforms", TestSelectionClipboard);
Run("DKZ compatibility, script retention and safe file import/export", TestSelectionFiles);
Run("text MAP sections, scripts, defaults and atomic rejection", TestTextMaps);
Run("text MAP projection and complete atomic editable snapshot", TestTextExport);
Run("original no-scenario MAP policy, malformed graphs, all scopes and exact complete snapshots",OriginalTextExportChecks.Run);
Run("complete bilingual ID catalogues and typed suggestions", TestValueCatalogues);
Run("project entity and nested band script ownership", TestProjectScopeCloning);
Run("sixteen-slot band editing, scope retention and all project versions", TestBandMembers);
Run("typed entity references, namespaces and atomic identity edits", TestEntityIdentities);
Run("read-only missing and ambiguous entity references, namespace/sentinel/active-slot guards",EntityReferenceDiagnosticChecks.Run);
DefinitionReferenceDiagnosticChecks.Run(Run);
CompilerGateFallbackChecks.Run(Run);
CompilerGameDateChecks.Run(Run);
CompilerDefaultGlobalChecks.Run(Run);
CompilerThingOrderChecks.Run(Run);
CompilerPlacementDiagnosticChecks.Run(Run);
CompilerActionBoundChecks.Run(Run);
CompilerReservedNameChecks.Run(Run);
NativeMissingGateConversionChecks.Run(Run);
TextGateIdentityChecks.Run(Run);
HistoricalScenarioCountChecks.Run(Run);
SelectionOwnerIdentityChecks.Run(Run);
DefinitionLayoutGuardChecks.Run(Run);
NativeDefinitionTailChecks.Run(Run);
Run("historical project section upgrades and atomic undo", TestProjectUpgrades);
Run("version-one scenario conversion and malformed upgrade rollback", TestFirstProjectScenarios);
Run("scenario graph insertion, scopes and reversible link editing", TestScriptGraphEditing);
Run("read-only scenario scope, cycle and historical compatibility diagnostics", TestScriptDiagnostics);
Run("all nine owner scopes, binding lifecycle and atomic ownership edits", TestScriptScopes);
Run("original scenario branch moves, subtree deletion and shared-link guards", TestScriptBranches);
Run("original sibling tree projection, shared context and iterative limits", TestScriptTreeProjection);
Run("clipped selection graphs, exact scope ownership and layout rollback", TestSelectionClipping);
Run("original bidirectional selection rotation, reflections and identity retention", TestSelectionTransforms);
Run("editor protected-border painting, world owners and special cut backgrounds", TestEditorTerrainRules);
Run("selection protection masks, original paste/transform policy and DKCP v3", TestSelectionProtection);
Run("historical selection original-reader defaults and exact native/project projections",HistoricalSelectionChecks.Run);
Run("source-compatible KMAP projection and exact raw selection terrain retention",SelectionTerrainProjectionChecks.Run);
Run("source selection object order, stable stacked creatures and transformed buffer retention",SelectionObjectOrderChecks.Run);
Run("original heart/portal/gate cursor footprints and constrained painting gestures", TestTerrainPlacement);
Run("read-only compiler terrain shapes, duplicate hearts and consecutive keepers", TestTerrainDiagnostics);
Run("original new-map presets, seven player defaults, versions and starter layouts", TestNewMapCreation);
Run("original area/gate/effect geometry, category IDs and atomic map interaction", TestMapRegions);
Run("source full region creation draft, repeated Add, atomic generation and guarded history",RegionCreationChecks.Run);
Run("private source region properties, nested options, equal acceptance and exact atomic history",RegionPropertiesSessionChecks.Run);
Run("source point/rectangle private region choice, cancellation, lifecycle and deferred atomic apply",RegionGeometryPickChecks.Run);
Run("private nested gate generation, source counters, full topology replay and parent cancel/apply",RegionGateGenerationChecks.Run);
Run("original patrol paths, loop occurrences, joins and coordinate-selected links", TestPatrolRoutes);
Run("source patrol editor acceptance counters, exact field-only undo and generic no-op retention",EditorPatrolCallerChecks.Run);
Run("source patrol tree grouping, exhaustive file-order graphs and independent packed Level10",()=>EditorPatrolGroupingChecks.Run(repoRoot));
Run("source patrol manager local occurrences, indirect warnings, guarded lifetime and atomic fields",EditorPatrolManagerChecks.Run);
Run("source patrol area selection, independent candidates, pause drafts, cancel and guarded acceptance",()=>EditorPatrolSelectionChecks.Run(FindRepoRoot()));
Run("source patrol warning requests, selection/all predicates, session flags and guarded read-only replies",EditorPatrolWarningChecks.Run);
Run("original hero-gate generator, templates, flags and atomic scenario additions", HeroGateGenerationChecks.Run);
Run("source patrol removal requests, default Yes/No, separate session flags and one guarded acceptance",EditorPatrolRemovalChecks.Run);
Run("original availability/pool/modifier/alliance matrices and exact batch undo", MapRuleChecks.Run);
Run("source patrol numeric pause draft, decimal prefix, range retry, cancel and one guarded acceptance",EditorPatrolPauseChecks.Run);
Run("original named global variables, ranges, 20.12 display and exact batch reset", MapVariableChecks.Run);
Run("map/shared rule and variable isolation, reference order and missing collections", MapVariableScopeChecks.Run);
Run("all nine AI profiles, source player defaults, resources and scope-safe reset", PlayerSettingsChecks.Run);
Run("all source manual AI settings, named options, room order, stale guards and exact undo",PlayerAIOptionsChecks.Run);
Run("source player camera/build coordinates, defaults, historical guards and exact undo", PlayerGeometryChecks.Run);
Run("read-only source map/player/band statistics, resources, caps and loaded definitions", MapStatisticsChecks.Run);
Run("source global definition defaults, editing and section-scoped resets", GlobalDefinitionChecks.Run);
Run("real 44/46-row creature 5449/5537 prefixes, every field/reset, opaque tails and malformed guards", NativeCreatureVariantChecks.Run);
Run("source definition units, signed attributes, exact fractional display and raw preservation", GlobalDefinitionNumericChecks.Run);
Run("source projectile forms, targets, signed parameters, inactive compiler fields and exact undo", GlobalProjectileChecks.Run);
Run("source creature jobs, four groups, warning override, signed tuples and exact undo", CreatureJobChecks.Run);
Run("source creature attack slots, named spells/melee, limits, inactive values and exact undo", CreatureAttackChecks.Run);
Run("source creature resistance/possession catalogues, whole-group drafts and exact undo", CreatureOptionChecks.Run);
Run("source door/trap/keeper-spell named rules, trigger parameters and atomic undo", GlobalRuleChecks.Run);
Run("source named effects, room slot masks, room objects and armour with exact atomic undo", GlobalDecorationChecks.Run);
Run("original complete terrain controls, source transformations/ranges/RGB Save and atomic guards", GlobalTerrainChecks.Run);
Run("original complete room controls, CantBuild/BattleEffect/masks/ranges/RGB and atomic guards", GlobalRoomChecks.Run);
Run("original complete door controls, source catalogues/ranges, disabled mana buddy/no input and atomic guards", GlobalDoorChecks.Run);
Run("original four remaining creature pages, all75 fields/45 rows, typed source rules, literal bytes and shared atomic context", CreaturePageChecks.Run);
Run("original complete trap parent/children, source masks/ranges/catalogues, hidden retention and atomic guards", GlobalTrapChecks.Run);
Run("original complete keeper spell parent/two children, source catalogues/ranges, integer upgrade recharge and atomic guards", GlobalSpellChecks.Run);
Run("source shared five-page context, 98 rows/two private spell slots, cancel, no-op and one guarded Undo", GlobalVariablesSessionChecks.Composition);
Run("source 2016 shared reset fields, current/all private resets, opaque scope and lifetime guards", GlobalVariablesSessionChecks.ResetsAndScope);
Run("shared global context preserves unused room ceiling/unedited arrays and rejects raw fingerprint changes", GlobalVariablesSessionChecks.OpaqueGuards);
Run("original complete creature spell page/private projectile, all41 rows/source ranges/literal bytes and shared atomic context", CreatureSpellChecks.Run);
Run("original nine-page creature shared context, private composition, job warning and one atomic Undo", CreatureGlobalsSessionChecks.Composition);
Run("source creature job warning Continue/Cancel, signed LONG totals and read-only session auto reply", CreatureJobsWarningChecks.Run);
Run("source retained creature Jobs page buffer, current-row callbacks and guarded common Apply", CreatureJobsPageContextChecks.Run);
Run("original nine-section current/all private resets over7471 values and lifetime/scope guards", CreatureGlobalsSessionChecks.ResetsAndGuards);
Run("shared creature inactive flags/unused slot, raw fingerprints and real44/46-row5449/5537 variants", CreatureGlobalsSessionChecks.OpaqueAndVariants);
Run("source effect-generator four slots, frequency, named choices and atomic undo", EffectGeneratorChecks.Run);

foreach (var argument in args)
{
    if (argument == "--field-catalog") PrintMissingFieldKeys();
    else ValidateExternalPath(argument);
}

if (reportPath is not null)
{
    // A separately requested report is a new artifact, never an overwrite of
    // an input map or an earlier run. Preserve complete failures, not a clipped
    // terminal tail, when exercising a large user map collection.
    Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
    using var report = new FileStream(reportPath,FileMode.CreateNew,FileAccess.Write,FileShare.None);
    System.Text.Json.JsonSerializer.Serialize(report,new { TimestampUtc = DateTime.UtcNow, Inputs = args, Passed = passedChecks, Failures = failures },new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine($"REPORT {reportPath}");
}

if (failures.Count == 0)
{
    Console.WriteLine("VALIDATION PASSED");
    return 0;
}

Console.Error.WriteLine($"VALIDATION FAILED ({failures.Count})");
foreach (var failure in failures)
{
    Console.Error.WriteLine($"- {failure}");
}

return 1;

void Run(string name, Action test)
{
    try
    {
        test();
        passedChecks.Add(name);
        Console.WriteLine($"PASS  {name}");
    }
    catch (Exception exception)
    {
        failures.Add($"{name}: {exception.Message}");
        Console.Error.WriteLine(exception);
        Console.Error.WriteLine($"FAIL  {name}");
    }
}

void TestSyntheticDescriptor()
{
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "Synthetic.kwd");
    var data = new byte[2368];
    WriteUInt32(data, 0, 0xDC);
    WriteUInt32(data, 4, 4);
    WriteUInt32(data, 8, (uint)data.Length);
    WriteUInt32(data, 12, 0xDD);
    WriteUInt32(data, 16, 0x1C);
    WriteUInt32(data, 48, 0xDF);
    WriteUInt32(data, 52, 2304);
    WriteUInt32(data, 2360, 0xDE);
    WriteUtf16(data, 56, 128, "Synthetic Keep");
    WriteUtf16(data, 2232, 128, "DK2 Validation");
    File.WriteAllBytes(path, data);

    var result = inspector.Inspect(path);
    Expect(result.Kind == Dk2FileKind.GameMapDescriptor, "KWD kind was not recognized.");
    Expect(result.MapName == "Synthetic Keep", "KWD name was not decoded.");
    Expect(result.Author == "DK2 Validation", "KWD author was not decoded.");
    Expect(result.IsValid, "A structurally valid KWD was rejected.");
}

void TestSyntheticMapTiles()
{
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "SyntheticMap.kld");
    const int width = 3;
    const int height = 2;
    var data = new byte[36 + (width * height * 4)];
    WriteUInt32(data, 0, 0x64);
    WriteUInt32(data, 4, 4);
    WriteUInt32(data, 8, (uint)data.Length);
    WriteUInt32(data, 12, 0x65);
    WriteUInt32(data, 16, 8);
    WriteUInt32(data, 20, width);
    WriteUInt32(data, 24, height);
    WriteUInt32(data, 28, 0x66);
    WriteUInt32(data, 32, width * height * 4);
    File.WriteAllBytes(path, data);

    var result = inspector.Inspect(path);
    Expect(result.Kind == Dk2FileKind.GameMapTiles, "Map.kld kind was not recognized.");
    Expect(result.Width == width && result.Height == height, "Map dimensions were decoded incorrectly.");
    Expect(result.IsValid, "A structurally valid Map.kld was rejected.");
}

void TestLegacyEditorMap()
{
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "Synthetic.dkm");
    var data = new byte[12];
    WriteUInt32(data, 0, 0x4650414D);
    WriteUInt32(data, 4, 1);
    WriteUInt32(data, 8, 0);
    File.WriteAllBytes(path, data);

    var result = inspector.Inspect(path);
    Expect(result.Kind == Dk2FileKind.EditorMap, "Legacy DKM kind was not recognized.");
    Expect(result.IsValid, "A valid DKM outer header was rejected.");
}

void TestCorruptSize()
{
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "CorruptMap.kld");
    var data = new byte[36];
    WriteUInt32(data, 0, 0x64);
    WriteUInt32(data, 4, 4);
    WriteUInt32(data, 8, 9999);
    File.WriteAllBytes(path, data);

    var result = inspector.Inspect(path);
    Expect(!result.IsValid, "A corrupt declared size was accepted.");
    Expect(result.Diagnostics.Any(item => item.Code == "DK2E141"), "The size diagnostic was not emitted.");
}

void TestLocalizationParity()
{
    var englishPath = Path.Combine(repoRoot, "src", "DK2MapEditor.App", "Localization", "Strings.en.xaml");
    var russianPath = Path.Combine(repoRoot, "src", "DK2MapEditor.App", "Localization", "Strings.ru.xaml");
    var english = ReadResourceKeys(englishPath);
    var russian = ReadResourceKeys(russianPath);

    var missingRussian = english.Except(russian).ToArray();
    var missingEnglish = russian.Except(english).ToArray();
    Expect(missingRussian.Length == 0, $"Missing Russian keys: {string.Join(", ", missingRussian)}");
    Expect(missingEnglish.Length == 0, $"Missing English keys: {string.Join(", ", missingEnglish)}");
    Expect(english.Count >= 60, "The localization catalog is unexpectedly small.");
    foreach (var type in LegacyEventSchemas.All) Expect(english.Contains("Event." + type.Name), "Missing event translation: " + type.Name);
}

void PrintMissingFieldKeys()
{
    var keys = ReadResourceKeys(Path.Combine(repoRoot, "src", "DK2MapEditor.App", "Localization", "Strings.en.xaml"));
    var fields = RecordTemplate.All.Where(template => template.Structure is not ("Trigger" or "Action")).SelectMany(template => LegacySchemas.Fields(template.Structure))
        .Concat(LegacySchemas.Fields("GAMEINFO")).Concat(LegacySchemas.Fields("GAMEBANDHERO")).Concat(LegacyGlobalSchemas.All.SelectMany(group => group.Fields))
        .Concat(new[] { "MAPPROPERTIES", "MAPOPTIONS", "MAPTHING", "MAPRECTEX", "MTHBAND", "TRIGGERLINK", "TRIGGER", "ACTION", "GLOBALTERRAIN", "GLOBALROOM", "GLOBALDOOR", "GLOBALTRAP", "GLOBALSPELL", "GLOBALCREATURE", "GLOBALCRSPELLS" }.SelectMany(LegacyProjectSchemas.Fields));
    var names = fields.Select(field => System.Text.RegularExpressions.Regex.Replace(FieldNaming.CanonicalName(field.Name), @"(?:\[\d+\])+$", "")).Distinct().Order().Where(name => !keys.Contains("Field." + name));
    Console.WriteLine("MISSING FIELD KEYS: " + string.Join(" | ", names));
}

void TestSchemas()
{
    Expect(LegacySchemas.Size("GAMEDATE") == 20, "Native date layout must be 20 bytes.");
    Expect(LegacySchemas.Size("GAMEPLAYER") == 205, "Native player layout must be 205 bytes.");
    Expect(LegacySchemas.Size("GAMEKEEPER") == 36, "Native keeper layout must be 36 bytes.");
    Expect(LegacySchemas.Field("GAMEINFO", "bMainOptions").Offset == 25540, "Native KWD options offset is wrong.");
    Expect(LegacySchemas.Field("GAMEINFO", "wstrMainName").Offset == 56, "Native KWD name offset is wrong.");
    Expect(LegacySchemas.Field("GAMEBAND", "bBandID").Offset == 42, "Hero band ID offset is wrong.");
    Expect(new NativeField("Gold", 0, 4, NativeValueKind.Int32).Read(new NativeField("Gold", 0, 4, NativeValueKind.Int32).Encode("-100")) == "-100", "Signed fields do not round-trip.");
}

void TestDrawing()
{
    Expect(MapDrawing.Rectangle(new(0, 0, 4, 3), false).Count() == 10, "Rectangle outline is wrong.");
    Expect(MapDrawing.Line(new(0, 0), new(3, 3)).Count() == 4, "Diagonal line is wrong.");
    Expect(MapDrawing.Ellipse(new(0, 0, 1, 1), true).Single() == new TilePoint(0, 0), "Single-tile ellipse is wrong.");
    var clip = new TileClipboard(3, 2, Enumerable.Range(1, 6).Select(value => new MapTile((byte)value, 2, 1)).ToArray());
    var rotated = clip.RotateClockwise();
    Expect(rotated.Width == 2 && rotated.Height == 3, "Rotation dimensions are wrong.");
    Expect(rotated.Tiles.Select(tile => tile.Terrain).SequenceEqual(new byte[] { 4, 1, 5, 2, 6, 3 }), "Rotation tile order is wrong.");
    var counterclockwise = clip.RotateCounterclockwise();
    Expect(counterclockwise.Width == 2 && counterclockwise.Height == 3 && counterclockwise.Tiles.Select(tile => tile.Terrain).SequenceEqual(new byte[] { 3, 6, 2, 5, 1, 4 }), "Counterclockwise rotation has wrong order/dimensions.");
    Expect(rotated.RotateCounterclockwise().Tiles.SequenceEqual(clip.Tiles) && counterclockwise.RotateClockwise().Tiles.SequenceEqual(clip.Tiles), "Opposite clipboard rotations are not inverses.");
    Expect(clip == clip with { }, "Clipboard record comparison failed.");
    var unrotated = rotated.RotateClockwise().RotateClockwise().RotateClockwise();
    Expect(unrotated.Tiles.SequenceEqual(clip.Tiles), "Four rotations do not recover the original.");
    Expect(clip.Mirror(true).Mirror(true).Tiles.SequenceEqual(clip.Tiles), "Mirror is not reversible.");
}

void TestNativeEditing()
{
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "PaintMap.kld");
    var data = new byte[36 + 4 * 4 * 4];
    WriteUInt32(data, 0, 0x64); WriteUInt32(data, 4, 4); WriteUInt32(data, 8, (uint)data.Length);
    WriteUInt32(data, 12, 0x65); WriteUInt32(data, 16, 8); WriteUInt32(data, 20, 4); WriteUInt32(data, 24, 4);
    WriteUInt32(data, 28, 0x66); WriteUInt32(data, 32, 64);
    for (var offset = 36; offset < data.Length; offset += 4) WriteUInt32(data, offset, new MapTile(2, 2, 1).Packed);
    File.WriteAllBytes(path, data);
    var document = NativeMapDocument.Load(path);
    document.Paint([new(1, 1), new(2, 1)], new(6, 3, 1));
    Expect(document.HasChanges && document.CanUndo, "Painting did not create an undo entry.");
    Expect(document.GetTile(1, 1).Terrain == 6 && document.GetTile(0, 0).Terrain == 2, "Painting escaped its target.");
    document.Undo(); Expect(!document.HasChanges && document.CanRedo, "Undo did not restore the exact original.");
    document.Redo(); Expect(document.GetTile(2, 1).Owner == 3, "Redo did not restore painting.");
    var beforeFailure = document.TileFile.Data.ToArray();
    try { document.Edit("Broken transaction", () => { document.Paint([new(0, 0)], new(5, 1, 2)); throw new InvalidOperationException("Simulated failure"); }); }
    catch (InvalidOperationException) { }
    Expect(document.TileFile.Data.Span.SequenceEqual(beforeFailure), "A failed transaction was not rolled back.");
    document.FloodFill(new(0, 0), new(3, 2, 1));
    Expect(document.GetTile(0, 0).Terrain == 3 && document.GetTile(1, 1).Terrain == 6, "Flood fill crossed a different tile.");
    var exportPath = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "export"));
    var reopened = NativeMapDocument.Load(exportPath);
    Expect(reopened.TileFile.Data.Span.SequenceEqual(document.TileFile.Data.Span), "Export did not preserve all bytes.");
    Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(data), "Editing or export changed the source file!");
    try { SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "export")); throw new Exception("Export overwrote an existing folder."); }
    catch (IOException) { }
}

void TestMalformedDimensions()
{
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "OversizeMap.kld");
    var data = new byte[36];
    WriteUInt32(data, 0, 0x64); WriteUInt32(data, 4, 4); WriteUInt32(data, 8, 36);
    WriteUInt32(data, 12, 0x65); WriteUInt32(data, 28, 0x66); WriteUInt32(data, 20, uint.MaxValue); WriteUInt32(data, 24, uint.MaxValue);
    File.WriteAllBytes(path, data);
    Expect(!inspector.Inspect(path).IsValid, "Oversize dimensions were accepted.");
}

void TestRecordEditing()
{
    using var fixture = new TemporaryFixture();
    var document = NativeMapDocument.Create(30, 25, "TestDungeon");
    Expect(document.Width == 30 && document.Height == 25, "New map dimensions are wrong.");
    foreach (var template in RecordTemplate.All)
    {
        var record = document.AddRecord(template, new(10, 12), 3);
        Expect(record.Length == template.Size, $"{template.Structure}: size is wrong.");
        var count = document.Records().Count;
        var duplicate = document.DuplicateRecord(record);
        Expect(document.Records().Count == count + (template.Structure == "GAMEBAND" ? 17 : 1), $"{template.Structure}: duplication failed.");
        var beforeUndo = record.File.Data.ToArray();
        document.Undo();
        Expect(document.Records().Count == count, $"{template.Structure}: duplication undo failed.");
        document.Redo();
        Expect(record.File.Data.Span.SequenceEqual(beforeUndo), $"{template.Structure}: duplication redo was not lossless.");
        if (template.FileSignature != 0xBE || !document.Files.Any(file => file.Signature == 0xD2 && file.Length > 60))
        {
            document.DeleteRecord(duplicate);
            Expect(document.Records().Count == count, $"{template.Structure}: deletion failed.");
            document.Undo();
            Expect(record.File.Data.Span.SequenceEqual(beforeUndo), $"{template.Structure}: deletion undo failed.");
        }
    }
    var exported = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "new-map"));
    var loaded = NativeMapDocument.Load(exported);
    Expect(loaded.Records().Count == document.Records().Count, "New record collections did not survive export.");
    foreach (var file in loaded.Files)
        Expect(SaveProjectionChecks.FileEquals(document,file,document.Files.Single(item => item.ExportPath == file.ExportPath).Data.ToArray()), $"New-map round-trip changed {file.ExportPath}.");
    var actionsFile = loaded.Files.Single(file => file.Signature == 0xD2);
    var oldActionCount = BinaryPrimitives.ReadUInt32LittleEndian(actionsFile.Data.Span[24..]);
    var action = loaded.Records().First(record => record.Category == "Record 0xD6");
    loaded.DeleteRecord(action);
    Expect(BinaryPrimitives.ReadUInt32LittleEndian(actionsFile.Data.Span[24..]) == oldActionCount - 1, "Deleting an action updated the wrong count field.");
}

void TestSelectionClipboard()
{
    var document = NativeMapDocument.Create(30, 30, "SelectionTest");
    var trigger = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Trigger"), new(0, 0));
    var action = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0));
    document.SetField(trigger, trigger.Fields.Single(field => field.Name == "ChildAnd"), action.Read(action.Fields.Single(field => field.Name == "ID")));
    var creature = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEKEEPER"), new(5, 6));
    document.SetField(creature, creature.Fields.Single(field => field.Name == "wKeeperTrigger"), trigger.Read(trigger.Fields.Single(field => field.Name == "ID")));
    document.Paint([new(5, 6)], new(6, 3, 1));
    var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    var rectangle = new TileRect(4, 4, 4, 4);
    var clipboard = document.CopySelection(rectangle);
    Expect(clipboard.ObjectCount == 1 && clipboard.ScriptCount == 2, "Clipboard omitted a creature or its script graph.");
    document.PasteSelection(new(10, 10), clipboard);
    var pasted = document.Records().Single(record => record.Category == "GAMEKEEPER" && record.Read(record.Fields.Single(field => field.Name == "dwKeeperLocationX")) == "11");
    Expect(pasted.Read(pasted.Fields.Single(field => field.Name == "dwKeeperLocationY")) == "12", "Pasted creature coordinates are wrong.");
    Expect(pasted.Read(pasted.Fields.Single(field => field.Name == "wKeeperTrigger")) != creature.Read(creature.Fields.Single(field => field.Name == "wKeeperTrigger")), "Pasted creature shares script state.");
    Expect(document.GetTile(11, 12).Terrain == 6, "Pasted tile did not align with the creature.");
    Expect(NativeMapValidator.Validate(document).All(item => item.Severity != DiagnosticSeverity.Error), "Pasted selection has dangling script links.");
    document.Undo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Selection paste undo was not atomic.");
    document.TransformSelection(rectangle, true);
    Expect(creature.Read(creature.Fields.Single(field => field.Name == "dwKeeperLocationX")) == "5" && creature.Read(creature.Fields.Single(field => field.Name == "dwKeeperLocationY")) == "5", "Rotation did not rotate creature coordinates.");
    Expect(document.GetTile(5, 5).Terrain == 6, "Rotated tiles do not align with rotated objects.");
    document.TransformSelection(rectangle, true); document.TransformSelection(rectangle, true); document.TransformSelection(rectangle, true);
    foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Four entity-aware rotations were not reversible.");
    document.CutSelection(rectangle, new(2, 2, 1));
    Expect(!document.Records().Any(record => record.Category == "GAMEKEEPER"), "Cut omitted placed objects.");
    document.Undo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Selection cut undo was not atomic.");
    var another = NativeMapDocument.Create(30, 30, "AnotherSelection");
    another.PasteSelection(new(10, 10), clipboard.Mirror(true));
    Expect(another.Records().Count(record => record.Category == "GAMEKEEPER") == 1, "Independent clipboard could not paste into another native map.");
    Expect(NativeMapValidator.Validate(another).All(item => item.Severity != DiagnosticSeverity.Error), "Cross-map clipboard produced invalid script links.");
    try { document.PasteSelection(new(29, 29), clipboard); throw new Exception("Selection paste silently clipped objects."); }
    catch (ArgumentOutOfRangeException) { }
    foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Rejected selection paste altered data.");
}

void TestValueCatalogues()
{
    var total = 0;
    foreach (var group in ValueCatalogues.Groups)
    {
        var source = LegacyCompilerRules.Elements(group); var choices = ValueCatalogues.Elements(group); total += choices.Count;
        Expect(choices.Count == source.Count, "Catalogue omits original entries: " + group);
        Expect(choices.Select(choice => choice.Value).Distinct().Count() == choices.Count, "Catalogue conflates IDs: " + group);
        for (var i = 0; i < source.Count; i++)
        {
            Expect(choices[i].Value == source[i].ID.ToString() && choices[i].Name("en").Length > 0 && choices[i].Name("ru").Any(character => character is >= 'А' and <= 'я'), "Catalogue ID/name mapping failed: " + source[i].Name);
            Expect(ValueCatalogues.Elements(group, true)[i].Value == source[i].Variable.ToString(), "Pool/action variable ID conflated with placed-element ID: " + source[i].Name);
        }
    }
    Console.WriteLine("      bilingual catalogue entries: " + total);
    Expect(ValueCatalogues.ForTemplate("Creature").Count == 45 && ValueCatalogues.Elements("BadElites").All(choice => choice.English.EndsWith(" (Elite)")), "Elite/hero catalogue identity lost.");
    Expect(ValueCatalogues.ArrayElement("CreaturesCount[29]")?.English == "Dwarf" && ValueCatalogues.ArrayElement("Rooms[0]")?.English == "Lair" && ValueCatalogues.ArrayElement("CreaturesParams[44][3]")?.English == "Prince Tristan", "Array indexes use IDs instead of source ordering.");
    var document = NativeMapDocument.Create(10, 10, "Catalogues");
    foreach (var template in RecordTemplate.All.Where(template => ValueCatalogues.ForTemplate(template.Structure).Count != 0))
    {
        var record = document.AddRecord(template, new(2, 2)); var field = ValueCatalogues.PrimaryField(record)!;
        var choices = ValueCatalogues.ForField(record, field); Expect(choices.Count > 0, "Missing record ID suggestions.");
        var before = record.File.Data.ToArray(); var id = choices.Last().Value;
        document.SetField(record, field, id); Expect(record.Read(field) == id, "Named selection did not set numeric ID.");
        document.Undo(); Expect(record.File.Data.Span.SequenceEqual(before), "Catalogue choice undo altered record data.");
        document.SetField(record, field, "255");
        Expect(!ValueCatalogues.ForField(record, field).Any(choice => choice.Value == "255") && record.Read(field) == "255", "Unknown ID was normalized on catalogue access.");
        document.Undo();
    }
    var availability = document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEVARTHINGSTATUS"), new(0, 0));
    var type = availability.Fields.Single(field => field.Name == "wThingType"); var idField = availability.Fields.Single(field => field.Name == "dwThingID");
    document.SetField(availability, type, "1");
    Expect(ValueCatalogues.ForField(availability, idField).First().Value == "2" && ValueCatalogues.ForField(availability, idField).First().English == "Lair", "Room availability catalogue must use variable ID 2, not terrain ID 11.");
    document.SetField(availability, type, "4"); Expect(ValueCatalogues.ForField(availability, idField).Count == 8, "Availability catalogue did not change with type.");
    var flag = new NativeField("Flag", 0, 1, NativeValueKind.Byte, 4);
    Expect(ValueCatalogues.ForField(availability, flag).Select(choice => choice.Value).SequenceEqual(["0", "1"]), "Flag suggestions are not boolean.");
    var parser = TextMapFile.Parse("GUI_GENERATED_MAP\nProject\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nEOF");
    using var fixture = new TemporaryFixture(); var path = Path.Combine(fixture.DirectoryPath, "project.dkm"); File.WriteAllBytes(path, parser.ToArray());
    var project = NativeMapDocument.Load(path);
    Expect(ValueCatalogues.GameVersion(project) == 13 && ValueCatalogues.GameVersion(document) == 17, "Game version confused with native file-format version.");
    var oldMagic = ValueCatalogues.ForTemplate("MagicalObject", 16).Single(choice => choice.Value == "127");
    var newMagic = ValueCatalogues.ForTemplate("MagicalObject", 17).Single(choice => choice.Value == "127");
    Expect(oldMagic.English != newMagic.English && oldMagic.Russian != newMagic.Russian, "Special item 0x7F version-dependent meaning lost.");
    foreach (var kind in new[] { "Creature", "Door", "Trap", "Object", "MagicalObject" })
    {
        var record = project.AddLegacyRecord(kind, new(1, 1)); var field = ValueCatalogues.PrimaryField(record)!;
        Expect(ValueCatalogues.ForField(record, field).Select(choice => choice.Value).SequenceEqual(ValueCatalogues.ForTemplate(kind).Select(choice => choice.Value)), "Project type-specific ID catalogue mismatch: " + kind);
        var original = record.File.Data.ToArray(); var last = ValueCatalogues.ForField(record, field).Last();
        project.SetField(record, field, last.Value); Expect(record.Read(field) == last.Value, "Named project ID edit failed.");
        project.Undo(); Expect(record.File.Data.Span.SequenceEqual(original), "Named project edit undo is not exact.");
    }
    var band = project.AddLegacyRecord("Band", new(1, 1));
    var sourceBandIDs = LegacyCompilerRules.Elements("BadCreatures").Concat(LegacyCompilerRules.Elements("Heros")).Select(item => item.ID.ToString()).ToArray();
    Expect(sourceBandIDs.Length == 32 && ValueCatalogues.BandCreatures.Select(choice => choice.Value).SequenceEqual(sourceBandIDs), "Band suggestions do not match the source's nonelite creature tree.");
    Expect(ValueCatalogues.ForField(band, band.Fields.Single(field => field.Name == "heros[0].id")).Count == 32 && ValueCatalogues.ForField(band, band.Fields.Single(field => field.Name == "heros[0].enemy")).Count == 7, "Nested hero-slot catalogue unavailable.");
    var nativeBand = document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEBAND"), new(2, 2));
    foreach (var candidate in new[] { (Document: project, Band: band), (Document: document, Band: nativeBand) })
    {
        candidate.Document.AddBandMember(candidate.Band, 34); // Imported/raw elite IDs remain editable and preserved.
        var member = candidate.Document.IsLegacyProject ? candidate.Band : candidate.Document.Records().Single(record => record.File == candidate.Band.File && record.Offset == candidate.Band.Offset + 51);
        var memberID = member.Fields.Single(field => field.Name == (candidate.Document.IsLegacyProject ? "heros[0].id" : "bGlobalCreatureID"));
        var snapshot = member.File.Data.ToArray();
        Expect(ValueCatalogues.ForField(member, memberID).Select(choice => choice.Value).SequenceEqual(sourceBandIDs) && member.Read(memberID) == "34"
            && member.File.Data.Span.SequenceEqual(snapshot), "Band catalogue normalized an imported elite or used the ordinary 45-entry context.");
    }
}

void TestProjectScopeCloning()
{
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "scopes.dkm");
    File.WriteAllBytes(path, TextMapFile.Parse("GUI_GENERATED_MAP\nScopes\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nEOF").ToArray());
    foreach (var kind in new[] { "Creature", "Door", "Object", "MagicalObject", "Area", "Band" })
    {
        var project = NativeMapDocument.Load(path);
        void Set(NativeRecord record, string name, string value) => project.SetField(record, record.Fields.Single(field => field.Name == name), value);
        string Read(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
        var owner = project.AddLegacyRecord(kind, new(1, 1));
        if (kind == "Band")
        {
            for (var index = 0; index < 2; index++)
            {
                Set(owner, $"heros[{index}].isvalid", "1"); Set(owner, $"heros[{index}].id", "6");
                Set(owner, $"heros[{index}].uniqueid", (100 + index).ToString());
            }
            // Future/inactive slot identities are also unique; keep their owned data.
            Set(owner, "heros[2].uniqueid", "102");
        }
        var condition = project.AddLegacyRecord("Trigger", new(0, 0));
        var action = project.AddLegacyRecord("Action", new(0, 0));
        var conditionID = Read(condition, "id"); var actionID = Read(action, "id");
        Set(condition, "and", actionID); Set(condition, "or", conditionID);
        var scope = kind switch { "Creature" => "2", "Door" => "64", "Object" => "128", "MagicalObject" => "256", "Area" => "4", _ => "8" };
        var target = kind switch { "Creature" => "creature", "Door" => "door", "Object" or "MagicalObject" => "object", "Area" => "area", _ => "band" };
        var identity = kind == "Area" ? "id" : "uniqueid";
        void Link(string scopeType, string field, string id)
        {
            var link = project.AddLegacyRecord("TriggerLink", new(0, 0));
            Set(link, "node.type", scopeType); Set(link, field, id); Set(link, "trigger", conditionID);
        }
        Link(scope, target, Read(owner, identity));
        if (kind == "Band") for (var index = 0; index < 3; index++) Link("16", "creature", Read(owner, $"heros[{index}].uniqueid"));
        // A different scope with an overlapping numeric ID must not be acquired/deleted.
        Link("1", "player", Read(owner, identity));
        var before = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var copy = project.DuplicateRecord(owner);
        var copiedIDs = copy.Fields.Where(field => field.Name.EndsWith("uniqueid", StringComparison.Ordinal)).Select(copy.Read).Where(id => id != "0").ToArray();
        Expect(copiedIDs.Distinct().Count() == copiedIDs.Length, kind + ": repeated copied unique IDs.");
        Expect(Read(copy, identity) != Read(owner, identity), kind + ": owner identity shared.");
        if (kind == "Band") for (var index = 0; index < 3; index++) Expect(Read(copy, $"heros[{index}].uniqueid") != Read(owner, $"heros[{index}].uniqueid"), "Nested band member shares identity.");
        var links = project.Records().Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)).ToArray();
        var copyLink = links.Single(record => Read(record, "node.type") == scope && Read(record, target) == Read(copy, identity));
        var clonedID = Read(copyLink, "trigger"); Expect(clonedID != conditionID, kind + ": copy shares original scenario.");
        var cloneCondition = project.Records().Single(record => record.Category == "Project.TRIGGER" && Read(record, "id") == clonedID);
        Expect(Read(cloneCondition, "or") == clonedID && Read(cloneCondition, "and") != actionID, "Scenario cycle/action links were not independently remapped.");
        if (kind == "Band")
            for (var index = 0; index < 3; index++) Expect(links.Single(record => Read(record, "node.type") == "16" && Read(record, "creature") == Read(copy, $"heros[{index}].uniqueid")).Read(copyLink.Fields.Single(field => field.Name == "trigger")) == clonedID, "Shared owned graph was cloned more than once or band scope was lost.");
        project.Undo(); foreach (var file in project.Files) Expect(file.Data.Span.SequenceEqual(before[file]), kind + ": scoped entity undo was not atomic.");
        project.Redo();
        copy = project.Records().Single(record => record.File == copy.File && record.Offset == copy.Offset);
        project.DeleteRecord(copy);
        links = project.Records().Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)).ToArray();
        Expect(links.Length == (kind == "Band" ? 5 : 2), kind + ": deleting a copy removed original or unrelated scope links.");
        Expect(links.Any(record => Read(record, "node.type") == "1") && links.Any(record => Read(record, "node.type") == scope && Read(record, "trigger") == conditionID), "Original or player scope links were deleted.");
        project.Undo(); Expect(project.Records().Count(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)) == (kind == "Band" ? 9 : 3), "Scoped deletion undo lost links.");
        var saved = SaveProjectionChecks.SaveCopy(project, Path.Combine(fixture.DirectoryPath, kind));
        var reopened = NativeMapDocument.Load(saved);
        Expect(reopened.Records().Count(record => record.Category is "Project.TRIGGER" or "Project.ACTION") == 4, "Scoped copy scripts lost during project save.");
        var compiled = reopened.CompileLegacyProject("CompiledScope", NativeMapDocument.Create(3, 3, "Basis"));
        var structure = kind switch { "Creature" => "GAMEKEEPER", "Door" => "GAMEDOOR", "Object" or "MagicalObject" => "GAMEOBJECT", "Area" => "GAMEAREA", _ => "GAMEBAND" };
        var compiledOwners = compiled.Records().Where(record => record.Category == structure).ToArray();
        Expect(compiledOwners.Length == 2, "Compilation lost duplicated scoped owners.");
        string TriggerID(NativeRecord record) => record.Read(record.Fields.Single(field => field.Name.EndsWith("Trigger", StringComparison.Ordinal)));
        Expect(TriggerID(compiledOwners[0]) == conditionID && TriggerID(compiledOwners[1]) == clonedID, kind + ": compilation attached the wrong scenario to the copied owner.");
        if (kind == "Band")
            for (var index = 0; index < 2; index++)
            {
                var slot = compiled.Records().Single(record => record.File == compiledOwners[1].File && record.Offset == compiledOwners[1].Offset + 51 + index * 32);
                Expect(TriggerID(slot) == clonedID, "Compiled nested hero is attached to original scenario.");
            }
    }
    var broken = NativeMapDocument.Load(path);
    var band = broken.AddLegacyRecord("Band", new(1, 1));
    try { broken.SetField(band, band.Fields.Single(field => field.Name == "heros[0].uniqueid"), band.Read(band.Fields.Single(field => field.Name == "uniqueid"))); throw new Exception("An existing unique identity was assigned to another member."); } catch (ArgumentException) { }
    var badIdentityFile = LegacySectionFile.Read(SaveProjectionChecks.SaveCopy(broken, Path.Combine(fixture.DirectoryPath, "bad-identity-seed")));
    var badIdentityPayload = badIdentityFile.Sections.Single(section => section.Signature == NativeMapDocument.LegacyBands).Payload.ToArray();
    WriteUInt32(badIdentityPayload, band.Fields.Single(field => field.Name == "heros[0].uniqueid").Offset, uint.Parse(band.Read(band.Fields.Single(field => field.Name == "uniqueid"))));
    var badIdentityPath = Path.Combine(fixture.DirectoryPath, "bad-identities.dkm");
    File.WriteAllBytes(badIdentityPath, LegacySectionFile.Create(badIdentityFile.Sections.Select(section => section.Signature == NativeMapDocument.LegacyBands ? section with { Payload = badIdentityPayload } : section)).ToArray());
    broken = NativeMapDocument.Load(badIdentityPath); band = broken.Records().Single(NativeMapDocument.IsBand);
    var unchanged = broken.Files.ToDictionary(file => file, file => file.Data.ToArray());
    try { broken.DuplicateRecord(band); throw new Exception("Repeated nested identities were accepted."); } catch (InvalidDataException) { }
    foreach (var file in broken.Files) Expect(file.Data.Span.SequenceEqual(unchanged[file]), "Rejected band copy changed bytes.");
    var dangling = NativeMapDocument.Load(path);
    var creature = dangling.AddLegacyRecord("Creature", new(1, 1));
    var missingLink = dangling.AddLegacyRecord("TriggerLink", new(0, 0));
    foreach (var (name, value) in new[] { ("node.type", "2"), ("creature", creature.Read(creature.Fields.Single(field => field.Name == "uniqueid"))), ("trigger", "999") })
        dangling.SetField(missingLink, missingLink.Fields.Single(field => field.Name == name), value);
    unchanged = dangling.Files.ToDictionary(file => file, file => file.Data.ToArray());
    try { dangling.DuplicateRecord(creature); throw new Exception("Dangling owned scenario was silently dropped."); } catch (InvalidDataException) { }
    foreach (var file in dangling.Files) Expect(file.Data.Span.SequenceEqual(unchanged[file]), "Rejected scripted copy changed bytes.");
}

void TestBandMembers()
{
    using var fixture = new TemporaryFixture();
    var native = NativeMapDocument.Create(8, 8, "Members");
    var band = native.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEBAND"), new(0, 0));
    foreach (var id in new uint[] { 6, 17, 42 })
    {
        var index = native.AddBandMember(band, id, 4, 3);
        var slot = native.Records().Single(record => record.File == band.File && record.Offset == band.Offset + 51 + index * 32);
        Expect(slot.Read(slot.Fields.Single(field => field.Name == "bGlobalBehavior")) == "45" && slot.Read(slot.Fields.Single(field => field.Name == "bHeroObjective")) == "19", "Historical new-member defaults changed.");
        var trigger = native.AddRecord(RecordTemplate.All.Single(template => template.Structure == "Trigger"), new(0, 0));
        native.SetField(slot, slot.Fields.Single(field => field.Name == "wHeroTrigger"), trigger.Read(trigger.Fields.Single(field => field.Name == "ID")));
    }
    var seed = SaveProjectionChecks.SaveCopy(native, Path.Combine(fixture.DirectoryPath, "native-seed"));
    var thingsPath = Path.Combine(fixture.DirectoryPath, "native-seed", band.File.ExportPath);
    var raw = File.ReadAllBytes(thingsPath);
    for (var index = 0; index < 3; index++) raw[band.Offset + 51 + index * 32 + 13] = (byte)(0xA0 + index);
    File.WriteAllBytes(thingsPath, raw); // Only generated fixtures, never a game map.
    native = NativeMapDocument.Load(seed); band = native.Records().Single(record => record.Category == "GAMEBAND");
    byte[] NativeSlot(int index) => band.File.Data.Span.Slice(band.Offset + 51 + index * 32, 32).ToArray();
    var originalSlots = Enumerable.Range(0, 3).Select(NativeSlot).ToArray();
    var before = native.Files.ToDictionary(file => file, file => file.Data.ToArray());
    native.MoveBandMember(band, 0, 2);
    Expect(NativeSlot(0).SequenceEqual(originalSlots[1]) && NativeSlot(1).SequenceEqual(originalSlots[2]) && NativeSlot(2).SequenceEqual(originalSlots[0]), "Member movement lost reserved bytes or scenarios.");
    native.Undo(); foreach (var file in native.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Native member move undo is not exact.");
    native.Redo(); before = native.Files.ToDictionary(file => file, file => file.Data.ToArray());
    native.RemoveBandMember(band, 1);
    Expect(NativeSlot(0).SequenceEqual(originalSlots[1]) && NativeSlot(1).SequenceEqual(originalSlots[0]) && native.BandMembers(band).Count(member => member.Active) == 2, "Native deletion did not compact full member records.");
    Expect(native.Records().Count(record => record.File.Signature == 0xD2) == 3, "Native deletion implicitly discarded script records.");
    native.Undo(); foreach (var file in native.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Native member removal undo is not exact.");
    native.Redo();
    for (var index = 2; index < 16; index++) Expect(native.AddBandMember(band, 17) == index, "Next active native slot is wrong.");
    before = native.Files.ToDictionary(file => file, file => file.Data.ToArray());
    try { native.AddBandMember(band, 17); throw new Exception("Seventeenth native band member was accepted."); } catch (InvalidOperationException) { }
    foreach (var file in native.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Capacity rejection changed native bytes.");
    var reopenedNative = NativeMapDocument.Load(SaveProjectionChecks.SaveCopy(native, Path.Combine(fixture.DirectoryPath, "native-edited")));
    Expect(reopenedNative.BandMembers(reopenedNative.Records().Single(record => record.Category == "GAMEBAND")).All(member => member.Active), "Sixteen-member native band did not reopen.");
    var firstNativeMember = native.Records().Single(record => record.File == band.File && record.Offset == band.Offset + 51);
    native.SetField(firstNativeMember, firstNativeMember.Fields.Single(field => field.Name == "bGlobalBehavior"), "0");
    before = native.Files.ToDictionary(file => file, file => file.Data.ToArray());
    try { native.RemoveBandMember(band, 0); throw new Exception("Partially configured native slot was silently removed."); } catch (InvalidOperationException) { }
    foreach (var file in native.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Native sparse-band rejection changed data.");
    Expect(TextMapFile.Project(native).Limitations.Any(loss => loss.Code == "Bands"), "Historical text projection failed to report the stopped native band sequence.");
    native.Undo();
    var baseProject = TextMapFile.Parse("GUI_GENERATED_MAP\nMembers\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nEOF");
    for (var version = 1; version <= 5; version++)
    {
        var legacyPath = Path.Combine(fixture.DirectoryPath, $"members-v{version}.dkm");
        var sections = baseProject.Sections.Where(section => section.Signature != NativeMapDocument.LegacyBands).Append(new(NativeMapDocument.LegacyBands, version, ReadOnlyMemory<byte>.Empty));
        File.WriteAllBytes(legacyPath, LegacySectionFile.Create(sections).ToArray());
        var project = NativeMapDocument.Load(legacyPath); var owner = project.AddLegacyRecord("Band", new(1, 1));
        for (var index = 0; index < 3; index++) Expect(project.AddBandMember(owner, (uint)(index + 6), index + 1, 3) == index, "Old project slot layout failed.");
        string Read(string name) => owner.Read(owner.Fields.Single(field => field.Name == name));
        string[]? identities = version == 1 ? null : Enumerable.Range(0, 3).Select(index => Read($"heros[{index}].uniqueid")).ToArray();
        if (identities is not null)
        {
            Expect(identities.Distinct().Count() == 3 && identities.All(id => id != "0" && id != Read("uniqueid")), "New band members share project IDs.");
            var condition = project.AddLegacyRecord("Trigger", new(0, 0));
            var conditionID = condition.Read(condition.Fields.Single(field => field.Name == "id"));
            foreach (var id in identities)
            {
                var link = project.AddLegacyRecord("TriggerLink", new(0, 0));
                project.SetField(link, link.Fields.Single(field => field.Name == "node.type"), "16");
                project.SetField(link, link.Fields.Single(field => field.Name == "creature"), id);
                project.SetField(link, link.Fields.Single(field => field.Name == "trigger"), conditionID);
            }
        }
        var snapshots = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
        project.MoveBandMember(owner, 0, 2);
        Expect(project.BandMembers(owner).Take(3).Select(member => member.CreatureID).SequenceEqual(new uint[] { 7, 8, 6 }), "Project member order failed.");
        if (identities is not null) Expect(Read("heros[2].uniqueid") == identities[0], "Moved project identity did not travel with its member.");
        project.Undo(); foreach (var file in project.Files) Expect(file.Data.Span.SequenceEqual(snapshots[file]), "Project move undo changed unknown bytes.");
        project.Redo(); snapshots = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
        project.RemoveBandMember(owner, 0);
        Expect(project.BandMembers(owner).Take(2).Select(member => member.CreatureID).SequenceEqual(new uint[] { 8, 6 }) && project.BandMembers(owner).Count(member => member.Active) == 2, "Project deletion did not compact slots.");
        if (identities is not null)
        {
            var links = project.Records().Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)).ToArray();
            Expect(links.Length == 2 && links.All(record => record.Read(record.Fields.Single(field => field.Name == "creature")) != identities[1]), "Removed member scope was retained or surviving member links were lost.");
        }
        project.Undo(); foreach (var file in project.Files) Expect(file.Data.Span.SequenceEqual(snapshots[file]), "Project member deletion undo was not atomic.");
        project.Redo();
        for (var index = 2; index < 16; index++) project.AddBandMember(owner, 17);
        snapshots = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
        try { project.AddBandMember(owner, 17); throw new Exception("Seventeenth project member was accepted."); } catch (InvalidOperationException) { }
        foreach (var file in project.Files) Expect(file.Data.Span.SequenceEqual(snapshots[file]), "Project capacity rejection changed bytes.");
        var saved = SaveProjectionChecks.SaveCopy(project, Path.Combine(fixture.DirectoryPath, $"edited-v{version}"));
        var reopened = NativeMapDocument.Load(saved);
        Expect(reopened.BandMembers(reopened.Records().Single(record => NativeMapDocument.IsBand(record))).All(member => member.Active), "Old band version did not preserve sixteen slots.");
        var container = LegacySectionFile.Read(saved);
        var badEnd = container.Sections.Single(section => section.Signature == NativeMapDocument.LegacyBands).Payload.ToArray();
        var sentinel = LegacyProjectSchemas.Fields(owner.Category[8..]).Single(field => field.Name == "heros[16].isvalid");
        WriteUInt32(badEnd, sentinel.Offset, 1);
        var badEndPath = Path.Combine(fixture.DirectoryPath, $"bad-end-v{version}.dkm");
        File.WriteAllBytes(badEndPath, LegacySectionFile.Create(container.Sections.Select(section => section.Signature == NativeMapDocument.LegacyBands ? section with { Payload = badEnd } : section)).ToArray());
        var invalidEnd = NativeMapDocument.Load(badEndPath);
        var invalidBand = invalidEnd.Records().Single(NativeMapDocument.IsBand);
        Expect(NativeMapValidator.Validate(invalidEnd).Any(diagnostic => diagnostic.Code == "DK2N010"), "Active seventeenth end slot was not diagnosed.");
        try { invalidEnd.MoveBandMember(invalidBand, 0, 1); throw new Exception("Invalid project end marker was ignored."); } catch (InvalidDataException) { }
        if (version == 5)
        {
            var compiled = reopened.CompileLegacyProject("CompiledBand", NativeMapDocument.Create(3, 3, "Basis"));
            Expect(compiled.BandMembers(compiled.Records().Single(record => NativeMapDocument.IsBand(record))).All(member => member.Active), "Compilation truncated the edited sixteen-member band.");
        }
        project.SetField(owner, owner.Fields.Single(field => field.Name == "heros[0].isvalid"), "0");
        Expect(NativeMapValidator.Validate(project).Any(diagnostic => diagnostic.Code == "DK2N009" && diagnostic.Severity == DiagnosticSeverity.Error), "Validator did not detect compiler-truncated band slots.");
        snapshots = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
        try { project.MoveBandMember(owner, 1, 2); throw new Exception("Sparse active band was silently compacted."); } catch (InvalidOperationException) { }
        foreach (var file in project.Files) Expect(file.Data.Span.SequenceEqual(snapshots[file]), "Sparse-band rejection changed bytes.");
    }
}

void TestScriptScopes()
{
    using var fixture = new TemporaryFixture();
    var basis = TextMapFile.Parse("GUI_GENERATED_MAP\nOwners\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nEOF");
    var path = Path.Combine(fixture.DirectoryPath, "owners.dkm"); File.WriteAllBytes(path, basis.ToArray());
    var timer = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
    foreach (var legacy in new[] { false, true })
    {
        var document = legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(48, 16, "Owners");
        if (legacy) document.Resize(48, 16, 0, 0, new(2, 2, 1));
        else for (byte player = 1; player <= 7; player++) if (player != 3) document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEPLAYER"), new(0, 0), player);
        for (byte player = 3; player <= 7; player++) document.Paint(MapDrawing.Rectangle(new(2 + (player - 3) * 6, 2, 5, 5), true), new(14, player, 1));
        void Set(NativeRecord record, string name, string value) => document.SetField(record, record.Fields.Single(field => field.Name == name), value);
        for (var copy = 0; copy < 2; copy++)
        {
            foreach (var (project, native) in new[] { ("Creature", "GAMEKEEPER"), ("Door", "GAMEDOOR"), ("Object", "GAMEOBJECT"), ("MagicalObject", "GAMEOBJECT"), ("Area", "GAMEAREA"), ("Band", "GAMEBAND"), ("Trap", "GAMETRAP"), ("Gate", "GAMEGATE"), ("Effect", "GAMEEFFECT") })
            {
                var record = legacy ? document.AddLegacyRecord(project, new(2 + copy, 9)) : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == native), new(2 + copy, 9));
                if (project == "MagicalObject") Set(record, legacy ? "id" : "bObjectID", ValueCatalogues.ForTemplate("MagicalObject", ValueCatalogues.GameVersion(document)).First().Value);
                if (project == "Band")
                {
                    if (copy == 0) Set(record, legacy ? "id" : "bBandID", "0");
                    document.AddBandMember(record, 6); document.AddBandMember(record, 17);
                }
            }
        }
        if (legacy)
            foreach (var record in document.Records())
                foreach (var field in record.Fields.Where(field => field.Name.EndsWith("uniqueid", StringComparison.Ordinal))) Set(record, field.Name, "0");
        var owners = document.ScriptOwners();
        Expect(owners.Select(owner => owner.Scope).Distinct().Order().SequenceEqual(new uint[] { 1, 2, 4, 8, 16, 32, 64, 128, 256 }), "The nine original owner scopes are not available.");
        Expect(owners.Count(owner => owner.Scope == 16) == 4, "Inactive hero slots were offered as scenario owners.");
        Expect(!owners.Any(owner => owner.Record.Category is "GAMETRAP" or "GAMEGATE" or "GAMEEFFECT" || owner.Record.File.Signature == NativeMapDocument.LegacyThings && owner.Record.Read(owner.Record.Fields.Single(field => field.Name == "node.type")) == "2"), "A non-scope entity was offered as a scenario owner.");
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> bytes)
        { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Scope editing rejection/undo/lookup changed unrelated bytes."); }
        var before = Snapshot(); var zeroOwner = owners.First(owner => owner.Scope == 2);
        try { document.BindScriptScope(zeroOwner, 60000); throw new Exception("An absent target was bound to a newly allocated owner."); } catch (ArgumentException) { }
        Exact(before);
        foreach (var scope in owners.Select(owner => owner.Scope).Distinct())
        {
            var owner = document.ScriptOwners().First(owner => owner.Scope == scope);
            before = Snapshot(); var root = document.EnsureScriptScope(owner);
            Expect(document.OwnerForScriptScope(root) is { } resolved && resolved.Record.File == owner.Record.File && resolved.Record.Offset == owner.Record.Offset && resolved.MemberIndex == owner.MemberIndex,
                "Created scope resolves to another physical owner.");
            if (legacy)
            {
                document.Undo(); Exact(before); document.Redo(); root = document.FindScriptScope(owner)!;
                if (scope == 16)
                    Expect(root.Owner.Read(root.Owner.Fields.Single(field => field.Name == "band")) == "0", "Band-zero member binding used its parent's unique ID instead of visible band ID.");
            }
            else Exact(before);
            before = Snapshot(); _ = document.EnsureScriptScope(owner); _ = document.ScriptBindings(); Exact(before);
            var id = ScriptGraph.ID(document.InsertScriptEvent(root.Owner, root.Field, timer));
            root = document.FindScriptScope(owner)!;
            var eventCount = ScriptGraph.Build(document).Events.Count;
            before = Snapshot(); document.RemoveScriptScope(root);
            Expect(ScriptGraph.Build(document).Events.Count == eventCount && (legacy ? document.FindScriptScope(owner) is null : document.FindScriptScope(owner)!.Target == 0), "Removing a binding deleted events or retained its owner link.");
            document.Undo(); Exact(before); root = document.FindScriptScope(owner)!;
            if (scope == 32) { Expect(document.ReassignScriptScope(root, owner).Target == id, "Moving a binding to itself changed it."); continue; }
            var destination = document.ScriptOwners().Last(candidate => candidate.Scope == scope);
            document.BindScriptScope(destination, id);
            before = Snapshot(); root = document.FindScriptScope(owner)!;
            try { document.ReassignScriptScope(root, destination); throw new Exception("An occupied destination binding was overwritten."); } catch (InvalidOperationException) { }
            Exact(before); document.RemoveScriptScope(document.FindScriptScope(destination)!);
            before = Snapshot(); root = document.FindScriptScope(owner)!;
            var moved = document.ReassignScriptScope(root, destination);
            Expect(moved.Target == id && (legacy ? document.FindScriptScope(owner) is null : document.FindScriptScope(owner)!.Target == 0)
                && ScriptGraph.Build(document).Events.Count == eventCount, "Scope reassignment lost events or retained its old binding.");
            document.Undo(); Exact(before); document.Redo();
            moved = document.FindScriptScope(destination)!;
            var incompatible = document.ScriptOwners().First(candidate => candidate.Scope != scope);
            before = Snapshot();
            try { document.ReassignScriptScope(moved, incompatible); throw new Exception("Moving a binding changed its scope."); } catch (ArgumentException) { }
            Exact(before);
        }
        var graph = ScriptGraph.Build(document);
        Expect(graph.Events.Count == 9 && graph.Roots.Count(root => root.Target != 0) == 9, "Owner lifecycle changed the number of connected graphs.");
        var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, legacy ? "scope-project" : "scope-native"));
        Expect(ScriptGraph.Build(NativeMapDocument.Load(saved)).Roots.Count(root => root.Target != 0) == 9, "Owner scopes were lost on save/reopen.");
        if (legacy)
        {
            var compiled = document.CompileLegacyProject("CompiledOwners", NativeMapDocument.Create(48, 16, "Basis"));
            var nativeGraph = ScriptGraph.Build(compiled);
            foreach (var root in graph.Roots.Where(root => root.Target != 0))
            {
                if(args.SequenceEqual(new[]{"--scope-api-details-only"}))
                {
                    Console.WriteLine($"SCOPE SOURCE scope={root.Scope} target={root.Target} owner={root.Owner.Category}@{root.Owner.Offset:X} fields="+string.Join(",",root.Owner.Fields.Where(field=>field.Name is "node.type" or "creature" or "door" or "object" or "area" or "band" or "player" or "trigger").Select(field=>field.Name+"="+root.Owner.Read(field))));
                    Console.WriteLine("SCOPE NATIVE roots="+string.Join(";",nativeGraph.Roots.Where(candidate=>candidate.Target!=0).Select(candidate=>$"{candidate.Scope}/{candidate.Target}/{candidate.Owner.Category}@{candidate.Owner.Offset:X}/{candidate.Field.Name}")));
                }
                Expect(nativeGraph.Roots.Count(candidate => candidate.Scope == root.Scope && candidate.Target == root.Target) == 1, "Compilation lost the exact owner of scope " + root.Scope);
            }
            document.AddLegacyRecord("TriggerLink", new(0, 0)); // Duplicate map binding, even though empty.
            before = Snapshot();
            try { document.EnsureScriptScope(document.ScriptOwners().Single(owner => owner.Scope == 32)); throw new Exception("Duplicate owner bindings were arbitrarily selected."); } catch (InvalidOperationException) { }
            Expect(NativeMapValidator.Validate(document).Any(item => item.Code == "DK2N018"), "Duplicate owner bindings were not diagnosed."); Exact(before);
            var orphanRoot = ScriptGraph.Build(document).Roots.Single(edge => edge.Scope == 2);
            Set(orphanRoot.Owner, "creature", "60000");
            Expect(NativeMapValidator.Validate(document).Any(item => item.Code == "DK2N017"), "A binding with no physical owner was not diagnosed.");
        }
    }
    // V2 supports player/creature/area/band/member roots, but not door/object fields.
    var v2 = LegacySectionFile.Create(basis.Sections.Where(section => section.Signature != NativeMapDocument.LegacyEvents)
        .Append(new(NativeMapDocument.LegacyEvents, 2, new byte[12 + 146 * 64])));
    var oldPath = Path.Combine(fixture.DirectoryPath, "scope-v2.dkm"); File.WriteAllBytes(oldPath, v2.ToArray());
    var old = NativeMapDocument.Load(oldPath); old.AddLegacyRecord("Door", new(1, 1)); old.AddLegacyRecord("Creature", new(1, 1));
    var oldBytes = old.Files.ToDictionary(file => file, file => file.Data.ToArray());
    try { old.EnsureScriptScope(old.ScriptOwners().Single(owner => owner.Scope == 64)); throw new Exception("A V2 scope acquired absent door fields."); } catch (InvalidOperationException) { }
    foreach (var file in old.Files) Expect(file.Data.Span.SequenceEqual(oldBytes[file]), "Unsupported V2 binding changed section bytes/counts.");
    var supported = old.EnsureScriptScope(old.ScriptOwners().Single(owner => owner.Scope == 2));
    Expect(supported.Owner.Category == "Project.TRIGGERLINKV2", "Supported V2 scope did not retain its original layout.");
    old.UpgradeLegacyProject(); supported = old.EnsureScriptScope(old.ScriptOwners().Single(owner => owner.Scope == 64));
    Expect(supported.Owner.Category == "Project.TRIGGERLINK", "Upgraded V2 scenario cannot bind a door.");
    for (var version = 1; version <= 5; version++)
    {
        var bandPath = Path.Combine(fixture.DirectoryPath, $"scope-band-v{version}.dkm");
        File.WriteAllBytes(bandPath, LegacySectionFile.Create(basis.Sections.Where(section => section.Signature != NativeMapDocument.LegacyBands)
            .Append(new(NativeMapDocument.LegacyBands, version, ReadOnlyMemory<byte>.Empty))).ToArray());
        var project = NativeMapDocument.Load(bandPath); var band = project.AddLegacyRecord("Band", new(0, 0));
        project.AddBandMember(band, 6); project.AddBandMember(band, 17);
        if (version == 1)
        {
            Expect(!project.ScriptOwners().Any(owner => owner.Scope is 8 or 16), "V1 bands without unique-ID fields were offered as bindable owners.");
            project.UpgradeLegacyProject();
        }
        var first = project.ScriptOwners().First(owner => owner.Scope == 16); var last = project.ScriptOwners().Last(owner => owner.Scope == 16);
        var memberRoot = project.EnsureScriptScope(first); var id = ScriptGraph.ID(project.InsertScriptEvent(memberRoot.Owner, memberRoot.Field, timer));
        memberRoot = project.FindScriptScope(first)!;
        project.ReassignScriptScope(memberRoot, last);
        Expect(project.FindScriptScope(last)!.Target == id, "Historical member scope could not move between slots.");
        project.UpgradeLegacyProject();
        var upgradedRoot = ScriptGraph.Build(project).Roots.Single(root => root.Scope == 16);
        Expect(project.OwnerForScriptScope(upgradedRoot)?.MemberIndex == 1 && upgradedRoot.Target == id, "Band upgrade changed the physical member owning a scenario.");
        var compiled = ScriptGraph.Build(project.CompileLegacyProject($"OldScope{version}", NativeMapDocument.Create(3, 3, "Basis")));
        Expect(compiled.Roots.Single(root => root.Scope == 16 && root.Target != 0).Target == id, "Historical member scenario did not compile after upgrade.");
    }
}

void TestScriptDiagnostics()
{
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "diagnostics.dkm");
    File.WriteAllBytes(path, TextMapFile.Parse("GUI_GENERATED_MAP\nDiagnostics\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nEOF").ToArray());
    var timer = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
    var allies = LegacyEventSchemas.All.Single(type => type.Name == "ACTION_ALLIES");
    var door = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_DOORDESTROYED");
    foreach (var legacy in new[] { false, true })
    {
        var document = legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(8, 8, "Diagnostics");
        // A declared keeper also needs the hero and neutral player slots.
        // Keep this a valid-map scenario fixture, rather than suppressing the
        // independent player-layout diagnostics now composed by the validator.
        if (!legacy)
            foreach (byte player in new byte[] { 1, 2 })
                document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEPLAYER"), new(0, 0), player);
        if (legacy) document.AddLegacyRecord("TriggerLink", new(0, 0));
        var root = ScriptGraph.Build(document).Roots.Single(edge => edge.Scope == 32);
        var conditionID = ScriptGraph.ID(document.InsertScriptEvent(root.Owner, root.Field, timer));
        var graph = ScriptGraph.Build(document);
        var child = graph.Events[conditionID].Edges.Single(edge => edge.Field.Name is "and" or "ChildAnd");
        var actionID = ScriptGraph.ID(document.InsertScriptEvent(child.Owner, child.Field, allies));
        IReadOnlyList<MapDiagnostic> Check()
        {
            var bytes = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var undo = document.CanUndo; var redo = document.CanRedo; var changes = 0;
            void Changed(object? sender, EventArgs args) => changes++;
            document.Changed += Changed;
            IReadOnlyList<MapDiagnostic> result;
            try { result = NativeMapValidator.Validate(document); }
            finally { document.Changed -= Changed; }
            foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(bytes[file]), "Diagnostics modified scenario bytes.");
            Expect(changes == 0 && undo == document.CanUndo && redo == document.CanRedo, "Diagnostics changed transaction history.");
            return result;
        }
        var initialDiagnostics=Check();
        Expect(!initialDiagnostics.Any(item => int.Parse(item.Code[4..]) >= 11), "Valid inserted graph has scenario semantic warnings: "+string.Join("; ",initialDiagnostics.Select(item=>item.Code+"/"+item.Severity+"/"+item.Message)));
        if (legacy)
        {
            graph = ScriptGraph.Build(document);
            var renamed = graph.Events[conditionID].Record;
            document.SetField(renamed, renamed.Fields.Single(field => field.Name == "id"), "111"); conditionID = 111;
            Expect(!Check().Any(item => item.Code == "DK2N014"), "Event ID remapping left a stale historical backlink.");
            // Direct numeric editing exposes stale historical backlinks for diagnosis.
            graph = ScriptGraph.Build(document); child = graph.Events[conditionID].Edges.Single(edge => edge.Field.Name == "and");
            document.SetField(child.Owner, child.Field, "0");
            Expect(Check().Any(item => item.Code == "DK2N014"), "Stale project backlink was not diagnosed.");
            document.Undo();
        }
        else
        {
            // Native ACTION.ChildAnd is reserved, NOT a scenario link.
            var record = ScriptGraph.Build(document).Events[actionID].Record;
            document.SetField(record, record.Fields.Single(field => field.Name == "ChildAnd"), "60000");
            Expect(!Check().Any(item => item.Code == "DK2N004"), "Reserved action bytes were diagnosed as a dangling link.");
            document.Undo();
        }
        graph = ScriptGraph.Build(document);
        var next = graph.Events[conditionID].Edges.Single(edge => edge.Field.Name is "or" or "NextOr");
        var siblingID = ScriptGraph.ID(document.InsertScriptEvent(next.Owner, next.Field, timer));
        graph = ScriptGraph.Build(document); child = graph.Events[siblingID].Edges.Single(edge => edge.Field.Name is "and" or "ChildAnd");
        document.SetScriptLink(child.Owner, child.Field, actionID);
        Expect(!Check().Any(item => item.Code is "DK2N011" or "DK2N014"), "A shared acyclic branch was mistaken for a cycle or a unique-parent backlink error.");
        document.SetField(child.Owner, child.Field, conditionID.ToString());
        Expect(Check().Count(item => item.Code == "DK2N011") == 1, "Cycle back edge was not diagnosed exactly once.");
        document.SetScriptLink(child.Owner, child.Field, 0);
        graph = ScriptGraph.Build(document); document.SetEventType(graph.Events[conditionID].Record, door);
        Expect(Check().Count(item => item.Code == "DK2N012" && item.Message.Contains($"#{conditionID} ")) == 1, "Original scope restriction was not diagnosed.");
        graph = ScriptGraph.Build(document); document.SetEventType(graph.Events[conditionID].Record, timer);
        var action = ScriptGraph.Build(document).Events[actionID].Record;
        if (legacy)
        {
            var importedID=action.Fields.Single(field=>field.Name=="id");
            var incoming=graph.Roots.Concat(document.Records().Where(ScriptGraph.IsEvent).SelectMany(ScriptGraph.Edges)).Where(edge=>edge.Target==actionID).ToArray();
            document.Edit("Literal imported wrong-sign action fixture",()=>
            {
                foreach(var edge in incoming)document.SetField(edge.Owner,edge.Field,"123");
                if(!System.Runtime.InteropServices.MemoryMarshal.TryGetArray(action.File.Data,out var bytes))throw new InvalidOperationException("Fixture payload unavailable.");
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.Array!.AsSpan(bytes.Offset+action.Offset+importedID.Offset),123);
            });actionID=123;
            Expect(Check().Any(item => item.Code == "DK2N013"), "Wrong-sign historical action ID was not diagnosed.");
            root = ScriptGraph.Build(document).Roots.Single(edge => edge.Scope == 32);
            document.SetField(root.Owner, root.Owner.Fields.Single(field => field.Name == "node.type"), "3");
            Expect(Check().Any(item => item.Code == "DK2N016"), "Combined/unverified scope was silently accepted.");
            document.Undo();
        }
        action = ScriptGraph.Build(document).Events[actionID].Record;
        document.SetField(action, action.Fields.Single(field => field.Name == (legacy ? "ctrl" : "Type")), "255");
        Expect(Check().Any(item => item.Code == "DK2N015"), "Unverified event type was not reported.");
        graph = ScriptGraph.Build(document); child = graph.Events[conditionID].Edges.Single(edge => edge.Field.Name is "and" or "ChildAnd");
        document.SetScriptLink(child.Owner, child.Field, 0);
        var actionNext = ScriptGraph.Build(document).Events[actionID].Edges.Single();
        document.SetField(actionNext.Owner, actionNext.Field, actionID.ToString());
        Expect(Check().Any(item => item.Code == "DK2N011"), "A cycle in an unlinked component was hidden.");
        document.SetField(actionNext.Owner, actionNext.Field, "60000");
        Expect(Check().Any(item => item.Code == "DK2N004" && item.Severity == DiagnosticSeverity.Error), "A dangling link in an unlinked component was hidden.");
    }
}

void TestScriptTreeProjection()
{
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "projection.dkm");
    File.WriteAllBytes(path, TextMapFile.Parse("GUI_GENERATED_MAP\nProjection\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nEOF").ToArray());
    var timer = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
    var action = LegacyEventSchemas.All.Single(type => type.Name == "ACTION_ALLIES");
    foreach (var legacy in new[] { false, true })
    {
        var document = legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(8, 8, "Projection");
        document.EnsureScriptScope(document.ScriptOwners().Single(owner => owner.Scope == 32));
        ScriptGraph Graph() => ScriptGraph.Build(document);
        ScriptEdge Root() => Graph().Roots.Single(edge => edge.Scope == 32);
        ScriptEdge Next(ushort id) => Graph().Events[id].Edges.Single(edge => edge.Field.Name is "NextOr" or "or" or "next");
        ScriptEdge Child(ushort id) => Graph().Events[id].Edges.Single(edge => edge.Field.Name is "ChildAnd" or "and");
        void Link(ScriptEdge edge, ushort id) => document.SetScriptLink(edge.Owner, edge.Field, id);
        ushort Add(LegacyEventSchemas.EventType type)
        {
            var record = legacy ? document.AddLegacyRecord(type.RecordSignature == 0xD5 ? "Trigger" : "Action", new(0, 0))
                : document.AddRecord(RecordTemplate.All.Single(item => item.Structure == (type.RecordSignature == 0xD5 ? "Trigger" : "Action")), new(0, 0));
            document.SetEventType(record, type); return ScriptGraph.ID(record);
        }
        var a = Add(timer); var b = Add(action); var c = Add(timer); var child = Add(action); var nested = Add(timer); var deep = Add(action);
        Link(Root(), a); Link(Next(a), b); Link(Next(b), c); Link(Child(a), child); Link(Next(child), nested); Link(Child(nested), deep);
        ScriptTreeProjection Project(int entries = 5000, int depth = 128) => ScriptTreeProjection.Build(Graph(), [Root()], entries, depth);
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); var changes = 0; document.Changed += (_, _) => changes++;
        var projection = Project(); var top = projection.Roots.Single().Children;
        Expect(top.Select(item => item.EventID).SequenceEqual(new[] { a, b, c }), "OR/next events are nested instead of siblings or a phantom continuation was added.");
        Expect(top[0].Children.Select(item => item.EventID).SequenceEqual(new[] { child, nested }) && top[0].Children[0].Children.Count == 0
            && top[0].Children[1].Children.Single().EventID == deep, "AND children or action leaves do not match the original tree.");
        Expect(top[1].IncomingEventID == a && top[2].IncomingEventID == b && top[0].Children[1].IncomingEventID == child, "Sibling entries lost the true incoming event identity.");
        Expect(top[2].Children.Single().Marker == ScriptTreeMarker.Empty && !projection.Truncated && projection.Orphans.Count == 0, "Empty child insertion context or rooted reachability is wrong.");
        projection = Project(depth: 0);
        Expect(projection.Truncated && projection.Orphans.Count == 0 && projection.Roots.Single().Children.Select(item => item.EventID).SequenceEqual(new[] { a, b, c }),
            "A child-depth limit hid siblings or relabelled connected descendants as orphaned.");
        projection = Project(entries: 3); Expect(projection.Truncated && projection.EntryCount == 3, "The tree entry budget was exceeded or not reported.");
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Tree projection edited map buffers.");
        Expect(changes == 0, "Read-only tree projection generated change/history notifications.");
        Link(Child(c), nested);
        projection = Project(); Expect(projection.Roots.Single().Children[2].Children.Single().Marker == ScriptTreeMarker.Shared, "A shared child was expanded again or mistaken for a cycle.");
        var cycle = Next(nested); document.SetField(cycle.Owner, cycle.Field, a.ToString());
        projection = Project();
        Expect(projection.Roots.Single().Children[0].Children.Single(item => item.EventID == a).Marker == ScriptTreeMarker.Cycle,
            "A cycle across child/sibling chains was not marked in its actual ancestor path.");
        Link(Next(nested), 0); Link(Child(c), 0);
        var missing = Child(c); document.SetField(missing.Owner, missing.Field, "60000");
        projection = Project(); var absent = projection.Roots.Single().Children[2].Children.Single();
        Expect(absent.Marker == ScriptTreeMarker.Missing && absent.EventID == 0 && absent.Incoming?.Target == 60000, "Missing tree targets became editable events or were hidden.");
        Link(Child(c), 0);
        var player = document.ScriptOwners().First(owner => owner.Scope == 1); var playerRoot = document.EnsureScriptScope(player); Link(playerRoot, a);
        projection = ScriptTreeProjection.Build(Graph());
        var sharedRoot = projection.Roots.Single(entry => entry.Incoming is { Scope: 1 } edge && edge.Target == a).Children.Single();
        Expect(sharedRoot.Marker == ScriptTreeMarker.Shared && sharedRoot.Incoming is { Scope: 1 }, "Shared root lost its distinct physical owner connection.");
        Link(document.FindScriptScope(player)!, 0);
        var orphanHead = Add(action); var orphanNext = Add(timer); var orphanChild = Add(action);
        Link(Next(orphanHead), orphanNext); Link(Child(orphanNext), orphanChild);
        projection = Project();
        Expect(projection.Orphans.Select(item => item.EventID).SequenceEqual(new[] { orphanHead, orphanNext })
            && projection.Orphans[0].Incoming is null && projection.Orphans[1].IncomingEventID == orphanHead && projection.Orphans[1].Children.Single().EventID == orphanChild,
            "Unlinked components were ordered by signed IDs rather than by their actual chain heads.");
        if (legacy) Expect(orphanHead > orphanNext, "The orphan ordering fixture does not exercise signed project IDs.");
    }
    // Large source fixtures exercise iterative traversal without thousands of
    // editing snapshots. This writes only a new temporary exported map.
    var basis = NativeMapDocument.Create(8, 8, "LongProjection");
    var prototype = basis.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0)); basis.SetEventType(prototype, action);
    var exported = SaveProjectionChecks.SaveCopy(basis, Path.Combine(fixture.DirectoryPath, "long-projection"));
    var saved = NativeMapDocument.Load(exported); var source = saved.Files.Single(file => file.Signature == 0xD2);
    var sample = source.Data.Span.Slice(60, 24).ToArray(); const int total = 6000;
    void Large(bool children)
    {
        var bytes = new byte[60 + total * 24]; source.Data.Span[..60].CopyTo(bytes);
        WriteUInt32(bytes, 8, (uint)bytes.Length); WriteUInt32(bytes, 20, children ? (uint)total : 0); WriteUInt32(bytes, 24, children ? 0 : (uint)total); WriteUInt32(bytes, 56, total * 24);
        for (var index = 0; index < total; index++)
        {
            var span = bytes.AsSpan(60 + index * 24, 24); sample.CopyTo(span);
            WriteUInt32(bytes, 60 + index * 24, children ? 0xD5u : 0xD6u);
            span[22] = children ? timer.Type : action.Type;
            BinaryPrimitives.WriteUInt16LittleEndian(span[16..], (ushort)(index + 1));
            BinaryPrimitives.WriteUInt16LittleEndian(span[(children ? 20 : 18)..], (ushort)(index == total - 1 ? 0 : index + 2));
        }
        File.WriteAllBytes(source.SourcePath, bytes);
        var document = NativeMapDocument.Load(exported); var root = ScriptGraph.Build(document).Roots.Single(edge => edge.Scope == 32); document.SetScriptLink(root.Owner, root.Field, 1);
        var graph = ScriptGraph.Build(document); root = graph.Roots.Single(edge => edge.Scope == 32);
        var projection = ScriptTreeProjection.Build(graph, [root], maximumEntries: 10000);
        if (!children) Expect(!projection.Truncated && projection.Roots.Single().Children.Count == total && projection.EntryCount == total + 1,
            "A long flat chain was truncated by child-depth limits or recursively nested.");
        else Expect(projection.Truncated && projection.Orphans.Count == 0 && projection.EntryCount < 200,
            "Deep child chains were not safely limited or were misclassified as orphaned.");
        projection = ScriptTreeProjection.Build(graph, [root]); Expect(projection.EntryCount <= 5000 && projection.Truncated, "Large graph budget enforcement failed.");
    }
    Large(false); Large(true);
}

void TestScriptBranches()
{
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "branches.dkm");
    File.WriteAllBytes(path, TextMapFile.Parse("GUI_GENERATED_MAP\nBranches\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nEOF").ToArray());
    var timer = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
    var action = LegacyEventSchemas.All.Single(type => type.Name == "ACTION_ALLIES");
    foreach (var legacy in new[] { false, true })
    {
        var document = legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(8, 8, "Branches");
        var mapOwner = document.ScriptOwners().Single(owner => owner.Scope == 32);
        document.EnsureScriptScope(mapOwner);
        ScriptGraph Graph() => ScriptGraph.Build(document);
        ScriptEdge Next(ushort id) => Graph().Events[id].Edges.Single(edge => edge.Field.Name is "NextOr" or "or" or "next");
        ScriptEdge Child(ushort id) => Graph().Events[id].Edges.Single(edge => edge.Field.Name is "ChildAnd" or "and");
        ScriptEdge Root() => Graph().Roots.Single(edge => edge.Scope == 32);
        void Link(ScriptEdge edge, ushort target) => document.SetScriptLink(edge.Owner, edge.Field, target);
        ushort Add(LegacyEventSchemas.EventType type)
        {
            var record = legacy ? document.AddLegacyRecord(type.RecordSignature == 0xD5 ? "Trigger" : "Action", new(0, 0))
                : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == (type.RecordSignature == 0xD5 ? "Trigger" : "Action")), new(0, 0));
            var id = ScriptGraph.ID(record); document.SetEventType(record, type); return id;
        }
        var a = Add(timer); var b = Add(action); var c = Add(timer); var d = Add(action);
        var child = Add(timer); var childNext = Add(action); var nested = Add(action); var existing = Add(action);
        Link(Root(), a); Link(Next(a), b); Link(Next(b), c); Link(Next(c), d);
        Link(Child(a), child); Link(Next(child), childNext); Link(Child(child), nested); Link(Child(c), existing);
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> snapshot, string context)
        { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(snapshot[file]), context); }
        byte[] Payload(ScriptEvent item)
        {
            var bytes = item.Record.File.Data.Span.Slice(item.Record.Offset, item.Record.Length).ToArray();
            foreach (var edge in item.Edges) bytes.AsSpan(edge.Field.Offset, edge.Field.Size).Clear();
            if (legacy) bytes.AsSpan(LegacyProjectSchemas.Size("NODE"), 2).Clear();
            return bytes;
        }
        void Reversible(Action edit, Action verify)
        {
            var before = Snapshot(); var payloads = Graph().Events.Values.ToDictionary(item => item.ID, Payload);
            edit(); verify(); var after = Snapshot();
            foreach (var item in Graph().Events.Values) Expect(Payload(item).AsSpan().SequenceEqual(payloads[item.ID]), "A branch operation changed IDs, event parameters or reserved bytes.");
            document.Undo(); Exact(before, "Branch operation undo was not one byte-exact transaction.");
            document.Redo(); Exact(after, "Branch operation redo changed preserved bytes."); verify(); document.Undo();
        }
        void Backlinks()
        {
            if (!legacy) return;
            var graph = Graph();
            foreach (var item in graph.Events.Values)
            {
                var incoming = graph.Roots.Concat(graph.Events.Values.SelectMany(entry => entry.Edges)).Where(edge => edge.Target == item.ID)
                    .Select(edge => ScriptGraph.IsEvent(edge.Owner) ? ScriptGraph.ID(edge.Owner) : (ushort)0).Distinct().ToArray();
                var previous = BinaryPrimitives.ReadUInt16LittleEndian(item.Record.File.Data.Span[(item.Record.Offset + LegacyProjectSchemas.Size("NODE"))..]);
                Expect(previous == (incoming.Length == 1 ? incoming[0] : 0), "A branch operation left a stale project backlink.");
            }
        }
        Reversible(() => { var edge = Child(c); document.MoveScriptBranch(a, edge.Owner, edge.Field); }, () =>
        {
            Expect(Root().Target == b && Next(b).Target == c && Child(c).Target == a && Next(a).Target == existing
                && Child(a).Target == child && Next(child).Target == childNext && Child(child).Target == nested && Next(c).Target == d,
                "Move carried following siblings, lost children or overwrote the destination chain."); Backlinks();
        });
        Reversible(() => document.MoveScriptBranch(b, ScriptBranchMove.Up), () =>
        { Expect(Root().Target == b && Next(b).Target == a && Next(a).Target == c && Child(a).Target == child, "Move up differs from original sibling ordering."); Backlinks(); });
        Reversible(() => document.MoveScriptBranch(a, ScriptBranchMove.Down), () =>
        { Expect(Root().Target == b && Next(b).Target == a && Next(a).Target == c && Child(a).Target == child, "Move down differs from original sibling ordering."); Backlinks(); });
        Reversible(() => document.MoveScriptBranch(a, ScriptBranchMove.Indent), () =>
        { Expect(Root().Target == b && Child(c).Target == a && Next(a).Target == existing, "Indent did not skip an action and prepend under the next condition."); Backlinks(); });
        Reversible(() => document.MoveScriptBranch(child, ScriptBranchMove.Outdent), () =>
        { Expect(Root().Target == child && Next(child).Target == a && Child(a).Target == childNext && Child(child).Target == nested, "Outdent lost sibling continuation or did not precede the parent."); Backlinks(); });
        Reversible(() => document.DeleteScriptBranch(a), () =>
        {
            var graph = Graph(); Expect(graph.Events.Count == 4 && Root().Target == b && Next(b).Target == c && Next(c).Target == d && Child(c).Target == existing,
                "Condition deletion did not remove exactly the selected AND subtree and retain the continuation.");
            Expect(new[] { a, child, childNext, nested }.All(id => !graph.Events.ContainsKey(id)), "A deleted subtree record remains."); Backlinks();
            var copy = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, (legacy ? "project" : "native") + "-branch-" + Guid.NewGuid().ToString("N")));
            Expect(ScriptGraph.Build(NativeMapDocument.Load(copy)).Events.Count == 4, "Branch deletion counts failed save/reopen.");
            if (legacy) Expect(ScriptGraph.Build(document.CompileLegacyProject("Branches", NativeMapDocument.Create(3, 3, "Basis"))).Events.Count == 4, "Deleted project subtree survived compilation.");
        });
        Reversible(() => document.DeleteScriptBranch(b), () =>
        { Expect(Next(a).Target == c && Graph().Events.Count == 7 && Child(a).Target == child, "Action deletion removed another event or did not splice its continuation."); Backlinks(); });
        void Reject(Action edit, string context)
        {
            var before = Snapshot(); var changes = 0; EventHandler handler = (_, _) => changes++; document.Changed += handler;
            try { try { edit(); throw new Exception(context); } catch (Exception exception) when (exception is InvalidOperationException or InvalidDataException) { } }
            finally { document.Changed -= handler; }
            Exact(before, context + " changed bytes."); Expect(changes == 0, context + " emitted a partial change notification.");
        }
        Reject(() => { var edge = Child(child); document.MoveScriptBranch(a, edge.Owner, edge.Field); }, "Moving into a descendant was allowed.");
        Reject(() => document.MoveScriptBranch(a, ScriptBranchMove.Up), "First sibling moved up past the root.");
        Reject(() => document.MoveScriptBranch(d, ScriptBranchMove.Down), "Last sibling moved down past its chain.");
        Reject(() => document.MoveScriptBranch(a, ScriptBranchMove.Outdent), "A top-level event moved out of its scope.");
        var unchanged = Snapshot(); var changed = 0; EventHandler noOpHandler = (_, _) => changed++; document.Changed += noOpHandler;
        var same = Root(); document.MoveScriptBranch(a, same.Owner, same.Field); document.Changed -= noOpHandler;
        Exact(unchanged, "Moving to the same connection was not a no-op."); Expect(changed == 0, "No-op movement generated history/notifications.");
        Link(Next(d), child);
        Reject(() => document.DeleteScriptBranch(a), "Deleting an externally shared child was allowed.");
        Link(Next(d), 0);
        var last = Next(d); document.SetField(last.Owner, last.Field, a.ToString());
        Reject(() => document.DeleteScriptBranch(a), "Deleting a cyclic branch was allowed.");
        Reject(() => document.MoveScriptBranch(a, ScriptBranchMove.Down), "Moving a cyclic branch was allowed.");
        Link(Next(d), 0);
        var missing = Next(d); document.SetField(missing.Owner, missing.Field, "60000");
        Reject(() => document.DeleteScriptBranch(a), "Deleting a branch with a dangling continuation was allowed.");
        Reject(() => document.MoveScriptBranch(a, ScriptBranchMove.Down), "Moving a branch with a dangling continuation was allowed.");
        Link(Next(d), 0);
        var player = document.ScriptOwners().First(owner => owner.Scope == 1);
        var playerRoot = document.EnsureScriptScope(player); Link(playerRoot, a);
        Reject(() => document.DeleteScriptBranch(a), "Deleting a shared root was allowed.");
        Reject(() => document.MoveScriptBranch(a, ScriptBranchMove.Down), "Moving a shared root was allowed.");
        playerRoot = document.FindScriptScope(player)!; Link(playerRoot, 0);
        Reversible(() => { var destination = document.FindScriptScope(player)!; document.MoveScriptBranch(a, destination.Owner, destination.Field); }, () =>
        {
            Expect(Root().Target == b && document.FindScriptScope(player)!.Target == a && Next(a).Target == 0 && Child(a).Target == child,
                "Cross-owner movement carried source siblings or lost children."); Backlinks();
            var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "branch-move-" + Guid.NewGuid().ToString("N")));
            var reopened = ScriptGraph.Build(NativeMapDocument.Load(saved));
            Expect(reopened.Roots.Single(edge => edge.Scope == 32).Target == b && reopened.Events[a].Edges.Single(edge => edge.Field.Name is "NextOr" or "or").Target == 0,
                "Moved branch failed save/reopen.");
            if (legacy) Expect(ScriptGraph.Build(document.CompileLegacyProject("MoveBranches", NativeMapDocument.Create(3, 3, "Basis"))).Events.Count == 8, "Moved project branch failed compilation.");
        });
        var door = legacy ? document.AddLegacyRecord("Door", new(1, 1)) : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == "GAMEDOOR"), new(1, 1));
        var doorOwner = document.ScriptOwners().Single(owner => owner.Scope == 64 && owner.Record.File == door.File && owner.Record.Offset == door.Offset);
        var doorRoot = document.EnsureScriptScope(doorOwner);
        var doorOnly = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_DOORDESTROYED");
        var doorEvent = ScriptGraph.ID(document.InsertScriptEvent(doorRoot.Owner, doorRoot.Field, doorOnly));
        Reject(() => { var root = Root(); document.MoveScriptBranch(doorEvent, root.Owner, root.Field); }, "A door-only branch was moved into map scope.");
        // Unknown native ACTION.ChildAnd is payload, never a branch or shared-link guard.
        if (!legacy)
        {
            var item = Graph().Events[b].Record;
            document.SetField(item, item.Fields.Single(field => field.Name == "ChildAnd"), a.ToString());
            Reversible(() => document.DeleteScriptBranch(a), () =>
                Expect(Graph().Events[b].Record.Read(Graph().Events[b].Record.Fields.Single(field => field.Name == "ChildAnd")) == a.ToString(), "Deleting a branch interpreted reserved ACTION bytes as links."));
        }
    }
}

void TestScriptGraphEditing()
{
    foreach (var type in LegacyEventSchemas.All) Expect(LegacyEventSchemas.AllowedScopes(type) != 0, "Original event scope catalogue is missing " + type.Name);
    var timer = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
    var actionType = LegacyEventSchemas.All.Single(type => type.Name == "ACTION_ALLIES");
    var doorOnly = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_DOORDESTROYED");
    Expect(LegacyEventSchemas.AllowedScopes(doorOnly) == 64 && (LegacyEventSchemas.AllowedScopes(timer) & 32) != 0, "Original scope masks changed.");
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "graph.dkm");
    File.WriteAllBytes(path, TextMapFile.Parse("GUI_GENERATED_MAP\nGraph\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nEOF").ToArray());
    foreach (var legacy in new[] { false, true })
    {
        var document = legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(8, 8, "Graph");
        if (legacy) document.AddLegacyRecord("TriggerLink", new(0, 0));
        var root = ScriptGraph.Build(document).Roots.Single(edge => edge.Scope == 32);
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var condition = document.InsertScriptEvent(root.Owner, root.Field, timer); var conditionID = ScriptGraph.ID(condition);
        var graph = ScriptGraph.Build(document);
        Expect(graph.Roots.Single(edge => edge.Scope == 32).Target == conditionID && graph.Events.Count == 1, "Scope event insertion did not connect its new root.");
        document.Undo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Event plus root insertion undo was not one atomic edit."); document.Redo();
        graph = ScriptGraph.Build(document);
        var child = graph.Events[conditionID].Edges.Single(edge => edge.Field.Name is "ChildAnd" or "and");
        var action = document.InsertScriptEvent(child.Owner, child.Field, actionType); var actionID = ScriptGraph.ID(action);
        if (legacy) Expect(actionID >= 32768 && conditionID <= 32767, "New project actions/conditions do not use original signed-short categories.");
        graph = ScriptGraph.Build(document);
        var sibling = graph.Events[actionID].Edges.Single();
        // Inserting a condition before an existing action shifts ACTION offsets in DKM.
        document.SetScriptLink(sibling.Owner, sibling.Field, 0);
        var later = document.InsertScriptEvent(sibling.Owner, sibling.Field, actionType); var laterID = ScriptGraph.ID(later);
        graph = ScriptGraph.Build(document); sibling = graph.Events[actionID].Edges.Single();
        var middle = document.InsertScriptEvent(sibling.Owner, sibling.Field, timer); var middleID = ScriptGraph.ID(middle);
        graph = ScriptGraph.Build(document);
        Expect(graph.Events[actionID].Edges.Single().Target == middleID
            && graph.Events[middleID].Edges.Single(edge => edge.Field.Name is "NextOr" or "or").Target == laterID, "Insertion lost the old chain or edited a shifted ACTION record.");
        if (legacy)
        {
            ushort Previous(ushort id)
            {
                var record = graph.Events[id].Record;
                return BinaryPrimitives.ReadUInt16LittleEndian(record.File.Data.Span[(record.Offset + LegacyProjectSchemas.Size("NODE"))..]);
            }
            Expect(Previous(actionID) == conditionID && Previous(middleID) == actionID && Previous(laterID) == middleID, "Project insertion did not maintain exact backlinks.");
        }
        before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var lastNext = graph.Events[laterID].Edges.Single();
        try { document.SetScriptLink(lastNext.Owner, lastNext.Field, conditionID); throw new Exception("A graph-editing cycle was introduced."); } catch (InvalidOperationException) { }
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Cycle rejection changed graph bytes.");
        root = graph.Roots.Single(edge => edge.Scope == 32);
        try { document.InsertScriptEvent(root.Owner, root.Field, doorOnly); throw new Exception("A door-only event was inserted in map scope."); } catch (InvalidOperationException) { }
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Scope rejection changed graph bytes.");
        try { document.SetScriptLink(lastNext.Owner, lastNext.Field, 60000); throw new Exception("A missing event was attached."); } catch (ArgumentException) { }
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Missing-target rejection changed graph bytes.");
        child = graph.Events[conditionID].Edges.Single(edge => edge.Field.Name is "ChildAnd" or "and");
        document.SetScriptLink(child.Owner, child.Field, 0);
        Expect(ScriptGraph.Build(document).Events.Count == 4 && ScriptGraph.Build(document).Reachable(conditionID).Count == 1, "Detach deleted records instead of retaining an unlinked component.");
        document.Undo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Detach/backlink undo was not exact.");
        document.Redo(); graph = ScriptGraph.Build(document); child = graph.Events[conditionID].Edges.Single(edge => edge.Field.Name is "ChildAnd" or "and");
        document.SetScriptLink(child.Owner, child.Field, actionID);
        Expect(!NativeMapValidator.Validate(document).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error), "Graph insertion made an invalid map.");
        if (legacy) _ = document.CompileLegacyProject("GraphCompile", NativeMapDocument.Create(3, 3, "Basis"));
        var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, legacy ? "project-graph" : "native-graph"));
        var reopened = ScriptGraph.Build(NativeMapDocument.Load(saved));
        Expect(reopened.Events.Count == 4 && reopened.Reachable(conditionID).Count == 4, "Edited graph did not survive save/reopen.");
        // Raw fields can still expose an existing cyclic/dangling graph for repair.
        graph = ScriptGraph.Build(document); lastNext = graph.Events[laterID].Edges.Single();
        document.SetField(lastNext.Owner, lastNext.Field, conditionID.ToString());
        Expect(ScriptGraph.Build(document).Reachable(conditionID).Count == 4 && ScriptGraph.Build(document).HasCycle(conditionID), "Cyclic graph indexing did not terminate or missed its cycle.");
        root = ScriptGraph.Build(document).Roots.Single(edge => edge.Scope == 32);
        before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        try { document.SetScriptLink(root.Owner, root.Field, conditionID); throw new Exception("A cyclic branch was attached to a root."); } catch (InvalidOperationException) { }
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Cyclic root-attachment rejection changed graph bytes.");
        document.SetScriptLink(lastNext.Owner, lastNext.Field, 0);
        document.SetField(lastNext.Owner, lastNext.Field, "60000");
        Expect(ScriptGraph.Build(document).Reachable(conditionID).Contains(60000), "Missing graph targets were hidden.");
        document.SetScriptLink(lastNext.Owner, lastNext.Field, 0);
        if (legacy)
        {
            graph = ScriptGraph.Build(document);
            var cloned = document.DuplicateRecord(graph.Events[conditionID].Record);
            var cloneRoot = ScriptGraph.ID(cloned); var clonedGraph = ScriptGraph.Build(document);
            foreach (var id in clonedGraph.Reachable(cloneRoot))
            {
                var item = clonedGraph.Events[id];
                Expect(item.Record.Category == "Project.ACTION" ? id >= 32768 : id <= 32767, "Graph duplication crossed signed event categories.");
            }
            document.Undo();
        }
    }
}

void TestFirstProjectScenarios()
{
    using var fixture = new TemporaryFixture();
    var basis = TextMapFile.Parse("GUI_GENERATED_MAP\nFirst scenarios\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nHERO_BANDS\n1\nHEROES 1 P1 0\nKNIGHT L6\nAREAS\n1\n8 1 1 1 1\nHERO_GATES\n1\n9 1 1\nEOF");
    byte[] Record(string structure, params (string, long)[] values)
    {
        var bytes = new byte[LegacyProjectSchemas.Size(structure)];
        foreach (var (name, value) in values)
        {
            var field = LegacyProjectSchemas.Fields(structure).Single(field => field.Name == name);
            field.Encode(value.ToString(System.Globalization.CultureInfo.InvariantCulture)).CopyTo(bytes, field.Offset);
        }
        return bytes;
    }
    var payload = new List<byte>();
    void Word(uint value) { var bytes = new byte[4]; WriteUInt32(bytes, 0, value); payload.AddRange(bytes); }
    Word(2); Word(0xC5); Word(4); payload.AddRange(Record("MAPACTIONPOINTV1", ("area", 8)));
    var conditionTypes = new[] { "TRIGGER_CLAIMAREA", "TRIGGER_CREAENTERAREA", "TRIGGER_SELECTALLAREA", "TRIGGER_TIMEELAPSED" };
    var actionTypes = new[] { "ACTION_RELEASEBAND", "ACTION_DESTROYGATE", "ACTION_SHOWAREA", "ACTION_ENABLEFOG", "ACTION_ADDCREATURE",
        "ACTION_CHANGESQUARE", "ACTION_ADDELEMENT", "ACTION_SHOWELEMENT", "ACTION_DISPLAYTIMER", "ACTION_SHOWNEXTROOM" };
    for (var type = 0; type < 4; type++)
    {
        Word(0xD5); Word(type == 0 ? 10u : 0);
        payload.AddRange(Record("MAPTRIGGERV1", ("node.type", type), ("reusable", type == 0 ? 1 : 0), ("player", 3), ("seconds", 800), ("squares", 8)));
        if (type != 0) continue;
        for (var action = 0; action < 10; action++)
        {
            Word(0xD6);
            payload.AddRange(Record("MAPACTIONV1", ("node.type", action), ("area", 8), ("gate", 9), ("band", 0), ("x", 2), ("y", 2),
                ("creatureid", 17), ("terrainid", 7), ("player", 3), ("level", 5), ("type", 2), ("element", 1), ("room", 9), ("seconds", 600), ("activate", 0)));
        }
    }
    Word(0xC5); Word(0); payload.AddRange(Record("MAPACTIONPOINTV1", ("area", 8))); // Empty scope remains empty.
    var firstInfo = basis.Sections.Single(section => section.Signature == NativeMapDocument.LegacyInfo).Payload.Span[..LegacyProjectSchemas.Size("MAPPROPERTIESV1")].ToArray();
    byte[] FileWith(byte[] scenario) => LegacySectionFile.Create(basis.Sections.Where(section => section.Signature is not (NativeMapDocument.LegacyInfo or NativeMapDocument.LegacyEvents))
        .Append(new(NativeMapDocument.LegacyInfo, 1, firstInfo)).Append(new(NativeMapDocument.LegacyEvents, 1, scenario))).ToArray();
    var path = Path.Combine(fixture.DirectoryPath, "v1-scenarios.dkm"); var original = FileWith(payload.ToArray()); File.WriteAllBytes(path, original);
    var document = NativeMapDocument.Load(path); var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    document.UpgradeLegacyProject();
    string Read(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
    var conditions = document.Records().Where(record => record.Category == "Project.TRIGGER").ToArray();
    var actions = document.Records().Where(record => record.Category == "Project.ACTION").ToArray();
    var links = document.Records().Where(record => record.Category == "Project.TRIGGERLINK").ToArray();
    Expect(conditions.Length == 4 && actions.Length == 10 && links.Length == 2 && Read(links[0], "trigger") == "1" && Read(links[1], "trigger") == "0", "Nested scenario counts/empty scopes changed.");
    for (var index = 0; index < conditions.Length; index++)
    {
        Expect(EntityReferences.EventType(conditions[index])?.Name == conditionTypes[index] && Read(conditions[index], "node.type") == "213", "Historical condition type conversion is wrong.");
        Expect(Read(conditions[index], "or") == (index == 3 ? "0" : (index + 2).ToString())
            && Read(conditions[index], "and") == (index == 0 ? "65535" : "0"), "Old condition/action chains or empty-action semantics changed.");
        Expect(BinaryPrimitives.ReadUInt16LittleEndian(conditions[index].File.Data.Span[(conditions[index].Offset + LegacyProjectSchemas.Size("NODE"))..]) == index, "Condition backlink not restored.");
    }
    for (var index = 0; index < actions.Length; index++)
    {
        Expect(EntityReferences.EventType(actions[index])?.Name == actionTypes[index] && Read(actions[index], "node.type") == "214", "Historical action type conversion is wrong.");
        Expect(Read(actions[index], "id") == (65535 - index).ToString() && Read(actions[index], "next") == (index == 9 ? "0" : (65534 - index).ToString()), "Descending historical action IDs/chain changed.");
        Expect(BinaryPrimitives.ReadUInt16LittleEndian(actions[index].File.Data.Span[(actions[index].Offset + LegacyProjectSchemas.Size("NODE"))..]) == (index == 0 ? 1 : 65536 - index), "Action backlink not restored.");
    }
    Expect(Read(conditions[0], "repeat") == "255" && Read(conditions[1], "repeat") == "1" && Read(conditions[0], "param5") == "8"
        && Read(conditions[1], "param5") == "1" && Read(conditions[3], "param5") == "800", "Old condition parameters changed.");
    Expect(Read(actions[0], "param1") == "0" && Read(actions[0], "param5") == "8" && Read(actions[1], "param5") == "9"
        && Read(actions[4], "param5") == "1" && Read(actions[4], "param7") == "1" && Read(actions[5], "param5") == "2"
        && Read(actions[6], "param4") == "1" && Read(actions[7], "param3") == "1" && Read(actions[8], "param1") == "16" && Read(actions[8], "param5") == "600", "Old action arguments/coordinate conversion changed.");
    Expect(!NativeMapValidator.Validate(document).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error), "Upgraded scenario has structural/script errors.");
    var compiled = document.CompileLegacyProject("FirstScenarios", NativeMapDocument.Create(3, 3, "Basis"));
    Expect(compiled.Records().Count(record => EntityReferences.EventType(record)?.RecordSignature == 0xD5) == 4
        && compiled.Records().Count(record => EntityReferences.EventType(record)?.RecordSignature == 0xD6) == 10, "Converted first-version scenarios did not compile.");
    var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "first-upgraded"));
    Expect(!NativeMapDocument.Load(saved).HasLegacyUpgrade && File.ReadAllBytes(path).AsSpan().SequenceEqual(original), "Scenario upgrade did not reopen or changed the input file.");
    document.Undo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Whole-project first-version upgrade undo failed.");
    var invalid = new List<byte[]> { payload.ToArray()[..^1], payload.Concat(new byte[] { 1 }).ToArray() };
    var badCount = payload.ToArray(); WriteUInt32(badCount, 0, uint.MaxValue); invalid.Add(badCount);
    var badMarker = payload.ToArray(); WriteUInt32(badMarker, 4, 0xC4); invalid.Add(badMarker);
    var badType = payload.ToArray();
    WriteUInt32(badType, 12 + LegacyProjectSchemas.Size("MAPACTIONPOINTV1") + 8 + LegacyProjectSchemas.Fields("MAPTRIGGERV1").Single(field => field.Name == "node.type").Offset, 99); invalid.Add(badType);
    foreach (var bytes in invalid)
    {
        var badPath = Path.Combine(fixture.DirectoryPath, "bad-" + Guid.NewGuid().ToString("N") + ".dkm"); File.WriteAllBytes(badPath, FileWith(bytes));
        var bad = NativeMapDocument.Load(badPath); var snapshot = bad.Files.ToDictionary(file => file, file => file.Data.ToArray());
        try { bad.UpgradeLegacyProject(); throw new Exception("Malformed historical scenario was upgraded."); } catch (InvalidDataException) { }
        foreach (var file in bad.Files) Expect(file.Data.Span.SequenceEqual(snapshot[file]), "Failed nested conversion partially upgraded metadata/scenarios.");
        Expect(!bad.CanUndo && !bad.HasChanges, "Failed nested conversion added history or dirty state.");
    }
    var activeSentinel = Record("MTHBANDV1", ("heros[16].isvalid", 1));
    foreach (var extra in new[]
    {
        new LegacySection(NativeMapDocument.LegacyBands, 1, activeSentinel),
        new LegacySection(NativeMapDocument.LegacyThings, 7, "FUTURE"u8.ToArray()),
        basis.Sections.Single(section => section.Signature == NativeMapDocument.LegacyInfo)
    })
    {
        var extraSections = LegacySectionFile.Parse(original).Sections.Where(section => section.Signature != extra.Signature || extra.Signature == NativeMapDocument.LegacyInfo).Append(extra);
        var badPath = Path.Combine(fixture.DirectoryPath, "bad-section-" + extra.Signature.ToString("X") + ".dkm"); File.WriteAllBytes(badPath, LegacySectionFile.Create(extraSections).ToArray());
        var bad = NativeMapDocument.Load(badPath); var snapshot = bad.Files.ToDictionary(file => file, file => file.Data.ToArray());
        try { bad.UpgradeLegacyProject(); throw new Exception("Unsafe historical section was upgraded."); } catch (InvalidDataException) { }
        foreach (var file in bad.Files) Expect(file.Data.Span.SequenceEqual(snapshot[file]), "Failed section conversion partially changed the project.");
        Expect(!bad.HasChanges && !bad.CanUndo, "Failed section conversion created an undo entry.");
    }
}

void TestProjectUpgrades()
{
    using var fixture = new TemporaryFixture();
    var basis = TextMapFile.Parse("GUI_GENERATED_MAP\nUpgrade\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nEOF");
    void Set(byte[] bytes, string structure, string name, object value)
    {
        var field = LegacyProjectSchemas.Fields(structure).Single(field => field.Name == name);
        field.Encode(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!).CopyTo(bytes, field.Offset);
    }
    string Read(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
    foreach (var (signature, current, versions) in new (uint, int, int)[]
    {
        (NativeMapDocument.LegacyInfo, 7, 6), (NativeMapDocument.LegacyPlayers, 3, 2),
        (NativeMapDocument.LegacyThings, 6, 5), (NativeMapDocument.LegacyBands, 5, 4), (NativeMapDocument.LegacyEvents, 3, 1)
    })
    for (var testVersion = 1; testVersion <= versions; testVersion++)
    {
        var version = signature == NativeMapDocument.LegacyEvents ? 2 : testVersion;
        var structure = signature switch
        {
            NativeMapDocument.LegacyInfo => version switch { 1 => "MAPPROPERTIESV1", 2 or 3 => "MAPPROPERTIESV2", _ => "MAPPROPERTIESV" + version },
            NativeMapDocument.LegacyPlayers => "MAPOPTIONSV" + version,
            NativeMapDocument.LegacyThings => version is 2 or 3 ? "MAPTHINGV2" : "MAPTHINGV" + version,
            NativeMapDocument.LegacyBands => "MTHBANDV" + version,
            _ => "TRIGGERLINKV2"
        };
        var stride = LegacyProjectSchemas.Size(structure); byte[] payload;
        if (signature == NativeMapDocument.LegacyInfo)
        {
            var currentInfo = basis.Sections.Single(section => section.Signature == signature).Payload.ToArray();
            Set(currentInfo, "MAPPROPERTIES", "Options", 41); Set(currentInfo, "MAPPROPERTIES", "Version", 16);
            payload = currentInfo.AsSpan(0, stride).ToArray();
            Set(payload, structure, "CreaturesParams[3][2]", 12345);
            if (version >= 4) Set(payload, structure, "Variables[155]", 1);
            if (version >= 5) { Set(payload, structure, "GameTicks", 8); Set(payload, structure, "Soundtrack", 3); Set(payload, structure, "Author", "Éditeur"); }
        }
        else if (signature == NativeMapDocument.LegacyPlayers)
        {
            payload = new byte[7 * stride];
            for (var index = 0; index < 7; index++)
            {
                var player = new byte[stride];
                Set(player, structure, version == 1 ? "Gold" : "PlayerGold", 4500 + index);
                Set(player, structure, version == 1 ? "Health" : "PlayerHealth", 100);
                Set(player, structure, "PlayerName", "Old player " + index);
                Set(player, structure, "CreaturesCount[4]", 20 + index);
                Set(player, structure, "Rooms[12]", 2);
                if (version == 1) { Set(player, structure, "AI", 99); Set(player, structure, "HeartX", 2); }
                else { Set(player, structure, "PlayerSpeed", 5); Set(player, structure, "PlayerCameraX", 99); }
                player.CopyTo(payload, index * stride);
            }
        }
        else if (signature == NativeMapDocument.LegacyThings)
        {
            payload = new byte[2 * stride];
            for (var index = 0; index < 2; index++)
            {
                var thing = new byte[stride];
                Set(thing, structure, "node.type", index == 0 ? 0 : 2); Set(thing, structure, "id", index == 0 ? 17 : 1);
                Set(thing, structure, "owner", 1); Set(thing, structure, "x", 1); Set(thing, structure, "y", 1);
                Set(thing, structure, "health", 100); Set(thing, structure, "dataAll", index == 0 ? 7 : 12);
                Set(thing, structure, version <= 3 ? "options" : "herosoptions", 23);
                if (version <= 3) Set(thing, structure, "behavior", 99);
                if (version > 1) { Set(thing, structure, "uniqueid", 600 + index); Set(thing, structure, "area", 4); Set(thing, structure, "objective", 11); }
                if (version >= 4) { Set(thing, structure, "creaturesoptions", 45); Set(thing, structure, "ennemy", 3); }
                thing.CopyTo(payload, index * stride);
            }
        }
        else if (signature == NativeMapDocument.LegacyBands)
        {
            payload = new byte[stride]; Set(payload, structure, "id", 0); Set(payload, structure, "name", "Historical band");
            for (var index = 0; index < 2; index++)
            {
                Set(payload, structure, $"heros[{index}].isvalid", 1); Set(payload, structure, $"heros[{index}].id", 17);
                Set(payload, structure, $"heros[{index}].level", 5 + index); Set(payload, structure, $"heros[{index}].health", 100);
                Set(payload, structure, $"heros[{index}].objective", 11);
                if (version >= 2) Set(payload, structure, $"heros[{index}].uniqueid", 700 + index);
                if (version >= 3) Set(payload, structure, $"heros[{index}].options2", 99);
            }
        }
        else
        {
            payload = new byte[12 + stride + 146 * 64]; WriteUInt32(payload, 0, 1);
            var link = new byte[stride]; Set(link, structure, "node.type", 4); Set(link, structure, "area", 8);
            link.CopyTo(payload, 12); Encoding.ASCII.GetBytes("Old variable alias").CopyTo(payload, 12 + stride);
        }
        var unknown = new LegacySection(0x12345678, 47, "OPAQUE UPGRADE TEST"u8.ToArray());
        var path = Path.Combine(fixture.DirectoryPath, $"upgrade-{signature:X}-{version}.dkm");
        var original = LegacySectionFile.Create(basis.Sections.Where(section => section.Signature != signature).Append(new(signature, version, payload)).Append(unknown)).ToArray();
        File.WriteAllBytes(path, original); var document = NativeMapDocument.Load(path);
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        Expect(document.HasLegacyUpgrade, "Historical section upgrade was not detected."); document.UpgradeLegacyProject();
        Expect(!document.HasLegacyUpgrade && document.HasChanges && document.CanUndo, "Upgrade did not become a reversible current-version project.");
        var records = document.Records().Where(record => record.File.Signature == signature).ToArray();
        Expect(BinaryPrimitives.ReadInt32LittleEndian(records[0].File.Data.Span[4..]) == current, "Upgrade header version was not updated.");
        if (signature == NativeMapDocument.LegacyInfo)
        {
            var info = records.Single();
            Expect(Read(info, "CreaturesParams[3][2]") == "12345" && Read(info, "Version") == (version == 6 ? "16" : "17"), "Historical metadata prefix/version changed semantics.");
            Expect(Read(info, "Options") == (version <= 2 ? "4" : "41"), "Version-specific metadata options were not interpreted like the original reader.");
            Expect(Read(info, "GameTicks") == (version >= 5 ? "8" : "4") && Read(info, "Soundtrack") == (version >= 5 ? "3" : "1"), "Metadata timing/music upgrade is wrong.");
            Expect(Read(info, "Variables[155]") == "10240" && Read(info, "Variables[158]") == "4915", "Missing original map variable defaults/fixes.");
            if (version >= 5) Expect(Read(info, "Author") == "Éditeur", "Upgrade damaged CP1252 metadata.");
        }
        else if (signature == NativeMapDocument.LegacyPlayers)
            for (var index = 0; index < 7; index++)
            {
                Expect(Read(records[index], "PlayerGold") == (4500 + index).ToString() && Read(records[index], "CreaturesCount[4]") == (20 + index).ToString()
                    && Read(records[index], "Rooms[12]") == "2" && Read(records[index], "PlayerName") == "Old player " + index, "Player conversion mixed rows or array positions.");
                Expect(Read(records[index], "PlayerCameraX") == "-1" && Read(records[index], "PlayerCameraY") == "-1", "Ignored old cameras were not reset.");
                Expect(Read(records[index], "PlayerSpeed") == (version == 1 ? "1" : "5"), "Version-one AI defaults or version-two settings changed.");
            }
        else if (signature == NativeMapDocument.LegacyThings)
        {
            Expect(Read(records[0], "herosoptions") == "23" && Read(records[0], "dataAll") == "7" && Read(records[0], "objective") == (version <= 3 ? "19" : "11"), "Old hero conversion differs from original.");
            Expect(Read(records[0], "creaturesoptions") == (version <= 3 ? "9" : "45") && Read(records[1], "dataAll") == (version <= 4 ? "4" : "12"), "Creature behavior/trap-shot compatibility changed.");
            if (version > 1) Expect(Read(records[0], "uniqueid") == "600" && Read(records[1], "uniqueid") == "601", "Thing unique identities changed.");
        }
        else if (signature == NativeMapDocument.LegacyBands)
        {
            var band = records.Single();
            Expect(Read(band, "id") == "0" && Read(band, "heros[0].level") == "5" && Read(band, "heros[1].level") == "6", "Band identity/member conversion failed.");
            Expect(Read(band, "heros[0].options2") == (version <= 3 ? "43" : "99") && Read(band, "heros[1].options2") == (version <= 3 ? "45" : "99"), "Original band leader/member behavior was not restored.");
            Expect(Read(band, "heros[0].objective") == (version <= 2 ? "19" : "11")
                && band.Read(LegacyProjectSchemas.Fields("MTHBAND").Single(field => field.Name == "heros[16].isvalid")) == "0", "Band objective/sentinel conversion failed.");
        }
        else Expect(Read(records.Single(record => record.Category == "Project.TRIGGERLINK"), "area") == "8"
            && Read(records.First(record => record.Category == "Project.VariableNames"), "Name") == "Old variable alias", "V2 links or trailing aliases shifted incorrectly.");
        var after = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        document.UpgradeLegacyProject(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(after[file]), "Upgrade is not idempotent.");
        document.Undo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Upgrade undo was not byte-exact.");
        Expect(!document.HasChanges && document.HasLegacyUpgrade, "Undo did not restore historical version status.");
        document.Redo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(after[file]), "Upgrade redo was not byte-exact.");
        var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, $"upgraded-{signature:X}-{version}"));
        var reopened = NativeMapDocument.Load(saved); Expect(!reopened.HasLegacyUpgrade, "Upgrade did not survive save/reopen.");
        Expect(LegacySectionFile.Read(saved).Sections.Single(section=>section.Signature==unknown.Signature).Version==unknown.Version && LegacySectionFile.Read(saved).Sections.Single(section=>section.Signature==unknown.Signature).Payload.Span.SequenceEqual(unknown.Payload.Span), "Opaque sections were changed by upgrade.");
        _ = reopened.CompileLegacyProject("UpgradeCompile", NativeMapDocument.Create(3, 3, "Basis"));
        Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(original), "Upgrade/export changed the source DKM.");
    }
}

void TestEntityIdentities()
{
    ScriptIdentitySafetyChecks.Run();
    Expect(EntityReferences.EventRules.Count == 14 && EntityReferences.EventRules.Count(rule => rule.Kind == EntityKind.Area) == 7
        && EntityReferences.EventRules.Count(rule => rule.Kind == EntityKind.Band) == 5 && EntityReferences.EventRules.Count(rule => rule.Kind == EntityKind.Effect) == 1
        && EntityReferences.EventRules.Count(rule => rule.Kind == EntityKind.Gate) == 1, "Original dialog reference bindings are incomplete.");
    Expect(!EntityReferences.EventRules.Any(rule => rule.Event.Name == "ACTION_GENERATE"), "Creature generator action was confused with an effect generator.");
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "references.dkm");
    File.WriteAllBytes(path, TextMapFile.Parse("GUI_GENERATED_MAP\nReferences\nPLAYERS 1\nMAP\n3 X 3\nOaOaOa\nOaOcOa\nOaOaOa\nEOF").ToArray());
    foreach (var legacy in new[] { false, true })
    {
        var document = legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(8, 8, "References");
        void Set(NativeRecord record, string name, string value) => document.SetField(record, record.Fields.Single(field => field.Name == name), value);
        string Read(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
        NativeRecord Add(string kind) => legacy ? document.AddLegacyRecord(kind, new(1, 1)) : document.AddRecord(RecordTemplate.All.Single(template => template.Structure == kind), new(1, 1));
        var area = Add(legacy ? "Area" : "GAMEAREA"); var band = Add(legacy ? "Band" : "GAMEBAND"); var effect = Add(legacy ? "Effect" : "GAMEEFFECT");
        foreach (var entity in new[] { area, band, effect }) Set(entity, EntityReferences.Identity(entity)!.Field.Name, "8");
        var otherArea = Add(legacy ? "Area" : "GAMEAREA"); Set(otherArea, legacy ? "nextid" : "bAreaNextID", "8");
        var hero = Add(legacy ? "Creature" : "GAMEHERO"); Set(hero, legacy ? "area" : "bHeroTargetArea", "8");
        document.AddBandMember(band, 17); document.AddBandMember(band, 6);
        var nested = legacy ? band : document.Records().Single(record => record.File == band.File && record.Offset == band.Offset + 51);
        Set(nested, legacy ? "heros[0].area" : "bHeroTargetArea", "8");
        if (!legacy) Set(hero, "bHeroBandID", "8");
        var gate = legacy ? Add("Gate") : null; if (gate is not null) Set(gate, "id", "8");
        // Every argument is the same number: only proven ID bindings may change.
        foreach (var type in LegacyEventSchemas.All)
        {
            var record = Add(type.RecordSignature == 0xD5 ? "Trigger" : "Action");
            document.SetEventType(record, type);
            record = document.Records().Single(item => item.File == record.File && item.Offset == record.Offset && item.Category == record.Category);
            foreach (var field in legacy ? record.Fields.Where(field => field.Name.StartsWith("param", StringComparison.Ordinal)) : type.Arguments) document.SetField(record, field, "8");
        }
        if (legacy)
        {
            Set(band, "uniqueid", "4000"); Set(band, "heros[0].uniqueid", "4001");
            NativeRecord Scope(string scope, string field, string id)
            {
                var link = Add("TriggerLink"); Set(link, "node.type", scope); Set(link, field, id); return link;
            }
            Scope("8", "band", "4000"); var memberScope = Scope("16", "creature", "4001"); Set(memberScope, "band", "8");
            Scope("1", "player", "4000");
            var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            Set(band, "uniqueid", "5000");
            var links = document.Records().Where(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal)).ToArray();
            Expect(Read(links.Single(record => Read(record, "node.type") == "8"), "band") == "5000"
                && Read(links.Single(record => Read(record, "node.type") == "16"), "band") == "8"
                && Read(links.Single(record => Read(record, "node.type") == "1"), "player") == "4000", "Unique identity edit crossed scope namespaces.");
            document.Undo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Unique identity undo was not atomic.");
            document.Redo(); Set(band, "heros[0].uniqueid", "5001");
            memberScope = document.Records().Single(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal) && Read(record, "node.type") == "16");
            Expect(Read(memberScope, "creature") == "5001" && Read(memberScope, "band") == "8", "Member unique identity edit changed its parent number.");
            before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            try { Set(band, "uniqueid", "5001"); throw new Exception("Member unique ID was acquired by its band."); } catch (ArgumentException) { }
            foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Rejected unique-ID collision changed bytes.");
        }
        foreach (var entity in new[] { area, band, effect }.Concat(gate is null ? [] : new[] { gate }))
        {
            var identity = EntityReferences.Identity(entity)!; var destination = identity.Kind == EntityKind.Band ? "0" : "9";
            var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            Set(entity, identity.Field.Name, destination);
            foreach (var reference in document.Records().SelectMany(EntityReferences.From).Where(reference => reference.Kind == identity.Kind))
            {
                var original = reference.Field.Read(before[reference.Record.File].AsSpan(reference.Record.Offset, reference.Record.Length));
                if (original == "8") Expect(reference.Record.Read(reference.Field) == destination, "Typed entity reference was not remapped: " + reference.Record.Category + "." + reference.Field.Name);
            }
            foreach (var record in document.Records().Where(record => EntityReferences.EventType(record) is not null))
            {
                var typed = EntityReferences.From(record).Where(reference => reference.Kind == identity.Kind).Select(reference => reference.Field).ToHashSet();
                var arguments = legacy ? record.Fields.Where(field => field.Name.StartsWith("param", StringComparison.Ordinal)) : EntityReferences.EventType(record)!.Arguments;
                foreach (var field in arguments.Where(field => !typed.Contains(field))) Expect(record.Read(field) == field.Read(before[record.File].AsSpan(record.Offset, record.Length)), "Numeric coincidence was incorrectly treated as a reference.");
            }
            document.Undo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Entity rename plus references undo was not atomic.");
            document.Redo();
            before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            if (identity.Kind == EntityKind.Gate)
            {
                document.DeleteRecord(entity);
                var expected = before[entity.File].Take(entity.Offset).Concat(before[entity.File].Skip(entity.Offset + entity.Length)).ToArray();
                BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(8), (uint)expected.Length - 12);
                Expect(entity.File.Data.Span.SequenceEqual(expected), "Source gate deletion changed more than its record and section size.");
                foreach (var file in document.Files.Where(file => file != entity.File)) Expect(file.Data.Span.SequenceEqual(before[file]), "Gate deletion rewrote its incoming action.");
                document.Undo();
            }
            else try { document.DeleteRecord(entity); throw new Exception("A referenced entity was deleted."); } catch (InvalidOperationException) { }
            foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Rejected entity deletion changed bytes.");
        }
        if (legacy)
        {
            var memberScope = document.Records().Single(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal) && Read(record, "node.type") == "16");
            Expect(Read(memberScope, "band") == "0" && Read(memberScope, "creature") == "5001", "Visible band ID zero lost its member scope.");
            var copy = document.DuplicateRecord(band);
            var copiedMemberScope = document.Records().Single(record => record.Category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal) && Read(record, "node.type") == "16" && Read(record, "creature") == Read(copy, "heros[0].uniqueid"));
            Expect(Read(copiedMemberScope, "band") == Read(copy, "id"), "Duplicated member scope retained its original band label.");
            document.Undo();
            var gem = Add("Object"); Set(gem, "id", "28");
            var compiled = document.CompileLegacyProject("ReferenceCompile", NativeMapDocument.Create(3, 3, "Basis"));
            var nativeBand = compiled.Records().Single(record => record.Category == "GAMEBAND");
            Expect(Read(nativeBand, "bBandID") == "0", "Band zero did not compile.");
            var camera = compiled.Records().Single(record => EntityReferences.EventType(record)?.Name == "ACTION_MOVECAMERA");
            Expect(Read(camera, "Param1") == "9", "Compiled camera target reverted to old area ID.");
        }
        var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, legacy ? "project-copy" : "native-copy"));
        var reopened = NativeMapDocument.Load(saved);
        Expect(reopened.Records().Select(EntityReferences.Identity).OfType<EntityIdentity>().Any(identity => identity.Kind == EntityKind.Band && identity.Record.Read(identity.Field) == "0"), "Band zero did not survive save/reopen.");
        var areaID = EntityReferences.Identity(area)!.Field;
        var snapshot = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        try { document.SetField(area, areaID, otherArea.Read(EntityReferences.Identity(otherArea)!.Field)); throw new Exception("Area identity collision was accepted."); } catch (ArgumentException) { }
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(snapshot[file]), "Rejected entity-ID collision changed bytes.");
        try { document.SetField(area, areaID, "0"); throw new Exception("Reserved area ID zero was accepted."); } catch (ArgumentException) { }
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(snapshot[file]), "Rejected reserved entity ID changed bytes.");
        var unknown = document.Records().First(record => EntityReferences.EventType(record)?.Name == "ACTION_MOVECAMERA");
        document.SetField(unknown, unknown.Fields.Single(field => field.Name == (legacy ? "ctrl" : "Type")), "255");
        snapshot = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        try { document.SetField(area, areaID, "10"); throw new Exception("Unverified event arguments were silently ignored during identity editing."); } catch (InvalidOperationException) { }
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(snapshot[file]), "Unknown-event identity remap failure changed bytes.");
        document.Undo();
        var text = NativeMapDocument.ImportTextMap(WriteTextProjection());
        Expect(text.Records().Any(record => NativeMapDocument.IsBand(record) && record.Read(record.Fields.Single(field => field.Name == "id")) == "0"), "Classic MAP projection/import dropped valid band zero.");
        string WriteTextProjection()
        {
            var textPath = Path.Combine(fixture.DirectoryPath, legacy ? "zero-project.map" : "zero-native.map");
            File.WriteAllText(textPath, TextMapFile.Project(document).Text, Encoding.ASCII); return textPath;
        }
    }
}

void TestTextExport()
{
    using var fixture = new TemporaryFixture(); var document = NativeMapDocument.Create(12, 12, "TextExport");
    void Set(NativeRecord record, string field, string value) => document.SetField(record, record.Fields.Single(item => item.Name == field), value);
    var metadata = document.Records().Single(record => record.Category == "Map"); Set(metadata, "wstrMainName", "Русское название");
    document.Paint([new(2, 2)], new(17, 3, 2)); // Bridge variant cannot survive the historical text grammar.
    var unknown = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEOBJECT"), new(4, 4)); Set(unknown, "bObjectID", "255");
    var creature = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEHERO"), new(3, 3)); Set(creature, "bGlobalCreatureID", "14"); Set(creature, "bGlobalLevel", "6");
    var area = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEAREA"), new(5, 5));
    var band = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEBAND"), new(0, 0));
    var hero = document.Records().Single(record => record.File == band.File && record.Offset == band.Offset + 51);
    Set(hero, "bGlobalCreatureID", "14"); Set(hero, "bGlobalLevel", "6"); Set(hero, "bGlobalPlayer", "3"); Set(hero, "bGlobalBehavior", "45");
    var gate = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEGATE"), new(4, 5)); Set(gate, "bGateType", "33");
    NativeRecord Event(string name)
    {
        var type = LegacyEventSchemas.All.Single(item => item.Name == name);
        var record = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == (type.RecordSignature == 0xD5 ? "Trigger" : "Action")), new(0, 0));
        document.SetEventType(record, type);
        return document.Records().Single(item => item.File == record.File && item.Offset == record.Offset);
    }
    var condition = Event("TRIGGER_TIMEELAPSED"); Set(condition, "Param1", "5"); Set(condition, "Param5", "600");
    var unsupported = Event("TRIGGER_VARIABLE"); Set(condition, "NextOr", unsupported.Read(unsupported.Fields.Single(item => item.Name == "ID")));
    var emptyCondition = Event("TRIGGER_TIMEELAPSED"); Set(emptyCondition, "Param1", "5"); Set(emptyCondition, "Param5", "900");
    Set(unsupported, "NextOr", emptyCondition.Read(emptyCondition.Fields.Single(item => item.Name == "ID")));
    Set(emptyCondition, "NextOr", condition.Read(condition.Fields.Single(item => item.Name == "ID"))); // Classic grammar cannot retain the back edge.
    var release = Event("ACTION_RELEASEBAND"); Set(release, "Param1", band.Read(band.Fields.Single(item => item.Name == "bBandID"))); Set(release, "Param5", area.Read(area.Fields.Single(item => item.Name == "bAreaID")));
    var destroy = Event("ACTION_DESTROYGATE");
    Set(destroy, destroy.Fields.Single(field => field.Offset == 12).Name, "5"); Set(destroy, destroy.Fields.Single(field => field.Offset == 14).Name, "6");
    Set(release, "NextOr", destroy.Read(destroy.Fields.Single(item => item.Name == "ID"))); Set(condition, "ChildAnd", release.Read(release.Fields.Single(item => item.Name == "ID")));
    var player = document.Records().Single(record => record.Category == "GAMEPLAYER"); Set(player, "bPlayerTrigger", condition.Read(condition.Fields.Single(item => item.Name == "ID")));
    var creatureCount = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEVARCREATURECOUNT"), new(0, 0)); Set(creatureCount, "dwGlobalPlayer", "3"); Set(creatureCount, "dwGlobalCreatureID", "7"); Set(creatureCount, "dwCreatureCount", "42");
    var availability = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEVARTHINGSTATUS"), new(0, 0)); Set(availability, "wGlobalPlayer", "3"); Set(availability, "wThingType", "4"); Set(availability, "dwThingID", LegacyCompilerRules.Elements("Doors").Single(item => item.AsciiName == "WOODEN_DOOR").Variable.ToString()); Set(availability, "dwThingState", "2");
    var snapshots = document.Files.ToDictionary(file => file.ExportPath, file => file.Data.ToArray()); var dirty = document.HasChanges;
    var projection = TextMapFile.Project(document);
    foreach (var code in new[] { "Metadata", "Terrain", "Things", "Scripts", "AdditionalSettings" }) Expect(projection.Limitations.Any(loss => loss.Code == code), "Missing text limitation: " + code);
    Expect(projection.Text.Contains("TRIGGER_TIME 600") && projection.Text.Contains("ACTION_HERO_RELEASE") && projection.Text.Contains("ACTION_DESTROY_HERO_GATE 1") && projection.Text.Contains("CREATURE_COUNT P1 GOBLIN 42"), "Native script/variable projection failed.");
    Expect(projection.Text.Contains("MUST_RESEARCH P1 WOODEN_DOOR") && projection.Text.Contains("TRIGGER_TIME 900\r\n0\r\n") && !projection.Text.Contains("TRIGGER_TIME 900\r\n0\r\n\r\nTRIGGER"), "Availability or historical zero-action line layout changed.");
    var result = SaveProjectionChecks.ExportText(Path.Combine(fixture.DirectoryPath, "text-export"), document);
    var imported = NativeMapDocument.Load(result.TextPath);
    Expect(imported.Records().Count(record => record.Category == "Project.TRIGGER") == 2 && imported.Records().Count(record => record.Category == "Project.ACTION") == 2, "Exported text lost supported script grammar.");
    Expect(imported.Records().Count(record => record.Category == "Project.MAPTHING") == 1, "Unrepresentable object was substituted for an unrelated object.");
    var editable = NativeMapDocument.Load(result.EditableCopyPath);
    Expect(editable.Files.Count == document.Files.Count && editable.Records().Count(record => record.File.Signature == 0xD2) == 5, "Editable snapshot lost unsupported script records.");
    foreach (var file in editable.Files) Expect(file.Data.Span.SequenceEqual(snapshots[file.ExportPath]), "Editable snapshot changed native bytes: " + file.ExportPath);
    foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(snapshots[file.ExportPath]), "Text projection mutated source data.");
    Expect(document.HasChanges == dirty && File.Exists(Path.Combine(Path.GetDirectoryName(result.TextPath)!, "Export-notes.txt")), "Text export changed dirty state or omitted the report.");
    var originalText = File.ReadAllBytes(result.TextPath);
    try { SaveProjectionChecks.ExportText(Path.GetDirectoryName(result.TextPath)!, document); throw new Exception("Existing text export was overwritten."); } catch (IOException) { }
    Expect(File.ReadAllBytes(result.TextPath).SequenceEqual(originalText) && !Directory.EnumerateDirectories(fixture.DirectoryPath, ".dk2-text-*").Any(), "Failed export left staging data or altered an existing file.");
}

void TestTextMaps()
{
    const string text = """
        GUI_GENERATED_MAP
        Text fixture
        PLAYERS 2
        MAP
        8 X 8
        OaOaOaOaOaOaOaOa
        OaOcOcOcOcOcOcOa
        OaOc1G1G1G1G1GOa
        OaOc1G1G1G1G1GOa
        OaOc1G1G1G1G1GOa
        OaOc1G1G1G1G1GOa
        OaOc1G1G1G1G1GOa
        OaOaOaOaOaOaOaOa
        THINGS
        2
        MI_RAISE_LEVEL 1 1
        TH_FLAG1 6 6
        CREATURES
        2
        GOBLIN 1 2 L3 P1
        DWARF 1 3 L10 PH
        DOORS
        1
        WOODEN_DOOR 1 4 P2 L
        TRAPS
        1
        TR_GAS 1 5 PN
        HEARTS_AND_GOLD
        KPR1_HEART_CENTER 4 4
        KPR2_HEART_CENTER -1 -1
        GOLD1 8000
        GOLD2 9000
        HERO_BANDS
        1
        HEROES 2 P1 7
        KNIGHT L6
        DWARF L2
        AREAS
        1
        8 1 1 1 2
        HERO_GATES
        1
        9 6 6
        TRIGGERS
        4
        TRIGGER_TIME 600
        2
        ACTION_HERO_RELEASE 7 G 9
        ACTION_ADD_ROOM P1 PRISON
        TRIGGER_TIME 900
        2
        ACTION_ADD_DOOR P2 MAGIC_DOOR
        ACTION_ADD_TRAP PN TR_GAS
        TRIGGER_CONTROL_AREA P1 8
        1
        ACTION_DESTROY_HERO_GATE 9
        TRIGGER_SLAP_COUNT_EQUALS 10
        1
        ACTION_ADD_SPELL PH HEAL_SPELL
        SCENARIO_DESCRIPTION
        CREATURE_COUNT P1 GOBLIN 42
        CREATURE_COUNT PH DWARF -1
        NO P1 PRISON
        MUST_RESEARCH P2 MAGIC_DOOR
        START_WITH PN TR_GAS
        EOF
        """;
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "fixture.map"); File.WriteAllText(path, text, Encoding.ASCII);
    var original = File.ReadAllBytes(path);
    var inspection = inspector.Inspect(path);
    Expect(inspection.Kind == Dk2FileKind.TextMap && inspection.IsValid && inspection.Width == 8, "Text MAP inspection failed: " + string.Join("; ", inspection.Diagnostics.Select(item => item.Message)));
    var document = NativeMapDocument.Load(path); var records = document.Records();
    string Value(NativeRecord record, string field) => record.Read(record.Fields.Single(item => item.Name == field));
    var info = records.Single(record => record.Category == "Project.MAPPROPERTIES");
    Expect(Value(info, "Name") == "Text fixture" && Value(info, "Players") == "2" && Value(info, "Version") == "13", "Text map metadata changed.");
    Expect(Value(info, "Variables[5]") == "-140" && Value(info, "Allies[1][2]") == "1", "Original map defaults were not restored.");
    var players = records.Where(record => record.Category == "Project.MAPOPTIONS").ToArray();
    Expect(Value(players[2], "PlayerGold") == "8000" && Value(players[3], "PlayerGold") == "9000", "Text starting gold lost.");
    Expect(Value(players[2], "CreaturesCount[1]") == "42" && Value(players[2], "Rooms[12]") == "1", "Text scenario availability lost.");
    Expect(Value(players[0], "PlayerSpeed") == "1" && Value(players[2], "PlayerCameraX") == "-1", "AI defaults or historical heart-hint semantics changed.");
    Console.WriteLine("      text importer AI defaults: " + LegacyCompilerRules.DefaultPlayerAI.Count);
    Expect(LegacyCompilerRules.DefaultPlayerAI.Count == 84, "Original default AI assignments are incomplete.");
    var things = records.Where(record => record.Category == "Project.MAPTHING").ToArray();
    Expect(things.Length == 6 && things.Select(record => Value(record, "uniqueid")).Distinct().Count() == 6, "Object import lost types or unique identities.");
    Expect(Value(things[2], "dataAll") == "3" && Value(things[3], "owner") == "1" && Value(things[4], "dataAll") == "1" && Value(things[5], "dataAll") == "4", "Creature/door/trap parameters are wrong.");
    var band = records.Single(record => record.Category == "Project.MTHBAND");
    Expect(Value(band, "heros[0].options2") == "43" && Value(band, "heros[1].options2") == "45" && Value(band, "heros[2].isvalid") == "0", "Band leader, count or slots changed.");
    var conditions = records.Where(record => record.Category == "Project.TRIGGER").ToArray();
    var actions = records.Where(record => record.Category == "Project.ACTION").ToArray();
    Expect(conditions.Length == 4 && actions.Length == 6 && records.Count(record => record.Category == "Project.TRIGGERLINK") == 2, "Text scenario graph import is incomplete.");
    Expect(Value(conditions[0], "or") == Value(conditions[1], "id") && Value(conditions[1], "or") == Value(conditions[3], "id"), "Player-scoped OR chain changed.");
    Expect(Value(conditions[0], "and") == Value(actions[0], "id") && Value(actions[0], "next") == Value(actions[1], "id"), "Action chain changed.");
    Expect(BinaryPrimitives.ReadUInt16LittleEndian(conditions[1].File.Data.Span[(conditions[1].Offset + LegacyProjectSchemas.Size("NODE"))..]) == ushort.Parse(Value(conditions[0], "id")) && BinaryPrimitives.ReadUInt16LittleEndian(actions[1].File.Data.Span[(actions[1].Offset + LegacyProjectSchemas.Size("NODE"))..]) == ushort.Parse(Value(actions[0], "id")), "Historical event backlinks were not retained for the original editor.");
    var generatedArea = records.Single(record => record.Category == "Project.MAPRECTEX" && Value(record, "node.type") == "0" && Value(record, "id") != "8");
    Expect(Value(actions[0], "param5") == Value(generatedArea, "id") && Value(generatedArea, "rect.x1") == "6", "Gate release was not translated to an area.");
    var diagnostics = NativeMapValidator.Validate(document);
    Expect(!diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error), "Imported project validation failed: " + string.Join("; ", diagnostics.Select(item => item.Message)));
    var zeroPath = Path.Combine(fixture.DirectoryPath, "zero-band.map");
    File.WriteAllText(zeroPath, text.Replace("HEROES 2 P1 7", "HEROES 2 P1 0").Replace("ACTION_HERO_RELEASE 7", "ACTION_HERO_RELEASE 0"), Encoding.ASCII);
    var zeroBand = NativeMapDocument.Load(zeroPath);
    Expect(zeroBand.Records().Any(record => NativeMapDocument.IsBand(record) && Value(record, "id") == "0")
        && zeroBand.Records().Any(record => EntityReferences.EventType(record)?.Name == "ACTION_RELEASEBAND" && Value(record, "param1") == "0"), "Historical MAP rejected a valid zero-numbered band or release action.");
    Expect(TextMapFile.Project(zeroBand).Text.Contains("ACTION_HERO_RELEASE 0", StringComparison.Ordinal), "Historical MAP projection lost a zero-band release action.");
    Expect(!document.HasChanges && !document.CanUndo && document.EditorChangeCount==0, "Ordinary text MAP opening differs from source Files_LoadMap clean state.");
    var explicitImport=NativeMapDocument.ImportTextMap(path);
    Expect(explicitImport.HasChanges&&!explicitImport.CanUndo&&explicitImport.Records().Count==records.Count
        &&explicitImport.TileFile.Data.Span.SequenceEqual(document.TileFile.Data.Span), "Explicit text MAP import lost its unsaved state/content or invented Undo.");
    var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "export")); var reopened = NativeMapDocument.Load(saved);
    SaveProjectionChecks.RoundTrip(document,reopened);
    var contentBefore=records.Where(record=>record.File.Signature!=ActivityStatisticsPayload.Signature).ToArray();
    var contentAfter=reopened.Records().Where(record=>record.File.Signature!=ActivityStatisticsPayload.Signature).ToArray();
    Expect(contentBefore.Length==contentAfter.Length&&contentBefore.Zip(contentAfter).All(pair=>pair.First.Category==pair.Second.Category
        &&pair.First.File.Data.Span.Slice(pair.First.Offset,pair.First.Length).SequenceEqual(pair.Second.File.Data.Span.Slice(pair.Second.Offset,pair.Second.Length))),
        "Imported MAP did not save/reopen with exact map/scenario/owner/opaque records.");
    Expect(File.ReadAllBytes(path).SequenceEqual(original), "Import/export overwrote the original text file.");
    var projection = TextMapFile.Project(document);
    var projected = SaveProjectionChecks.ExportText(Path.Combine(fixture.DirectoryPath, "text-copy"), document);
    var importedProjection = NativeMapDocument.Load(projected.TextPath);
    Expect(importedProjection.Records().Count(record => record.Category == "Project.TRIGGER") == 4 && importedProjection.Records().Count(record => record.Category == "Project.ACTION") == 6, "Classic text export dropped supported conditions/actions.");
    var exactProject = NativeMapDocument.Load(projected.EditableCopyPath);
    Expect(SaveProjectionChecks.PreviousEquals(document,File.ReadAllBytes(saved)), "Full project snapshot is not exact.");
    SaveProjectionChecks.RoundTrip(document,exactProject);
    Expect(!projection.Limitations.Any(loss=>loss.Code=="Scripts")
        &&exactProject.Records().Count(record=>record.File.Signature!=ActivityStatisticsPayload.Signature)==contentBefore.Length,
        "Text-compatible scripts/opaque content incorrectly omitted; statistics counted separately.");
    File.WriteAllText(Path.Combine(fixture.DirectoryPath, "utf8.map"), text.Replace("Text fixture", "Éditeur"), new UTF8Encoding(true));
    Expect(NativeMapDocument.Load(Path.Combine(fixture.DirectoryPath, "utf8.map")).Records().Any(record => record.Category == "Project.MAPPROPERTIES" && Value(record, "Name") == "Éditeur"), "BOM-marked UTF-8 Latin metadata was lost.");
    File.WriteAllBytes(Path.Combine(fixture.DirectoryPath, "bad.map"), [0xEF, 0xBB, 0xBF, 0xFF]);
    Expect(!inspector.Inspect(Path.Combine(fixture.DirectoryPath, "bad.map")).IsValid, "Malformed UTF-8 escaped the inspector.");
    File.WriteAllText(Path.Combine(fixture.DirectoryPath, "cyrillic.map"), text.Replace("Text fixture", "Русская карта"), new UTF8Encoding(true));
    Expect(!inspector.Inspect(Path.Combine(fixture.DirectoryPath, "cyrillic.map")).IsValid, "Legacy CP1252 name silently lost unrepresentable characters.");
    foreach (var invalid in new[]
    {
        text.Replace("GOBLIN 1 2", "UNKNOWN 1 2"), text.Replace("L3 P1", "L99 P1"), text.Replace("8 X 8", "1025 X 8"),
        text.Replace("TH_FLAG1 6 6", "TH_FLAG1 99 6"), text.Replace("HEROES 2 P1 7", "HEROES 17 P1 7"),
        text.Replace("TRIGGER_CONTROL_AREA P1 8", "TRIGGER_CONTROL_AREA P1 99"), text.Replace("ACTION_HERO_RELEASE 7", "ACTION_HERO_RELEASE 99"),
        text.Replace("EOF", "UNKNOWN_SECTION"), text + "\nUNEXPECTED", text.Replace("MI_RAISE_LEVEL 1 1", "MI_RAISE_LEVEL 1 1 ignored"),
        text.Replace("THINGS", "THINGS\n-1\nTHINGS")
    })
    {
        try { TextMapFile.Parse(invalid); throw new Exception("Malformed text MAP was partially accepted."); }
        catch (InvalidDataException exception) { Expect(exception.Message.Contains("line", StringComparison.OrdinalIgnoreCase), "Missing line-number diagnostic."); }
    }
    var tiles = TextMapFile.Parse("GUI_GENERATED_MAP\nTiles\nPLAYERS 1\nMAP\n3 X 3\nOmOrOs\nHAHBOT\n1A1B1T\nEOF").ReadTiles().Tiles;
    Expect(tiles[0] == new MapTile(36, 1, 1) && tiles[1] == new MapTile(37, 1, 1) && tiles[2] == new MapTile(37, 1, 2) && tiles[5] == new MapTile(31, 2, 1) && tiles[8] == new MapTile(32, 3, 1), "Historical tile aliases, owners or variants changed.");
}

void TestPatrolRoutes()
{
    using var fixture = new TemporaryFixture();
    foreach (var legacy in new[] { false, true })
    {
        var document = NativeMapDocument.CreateNewMap(new("Patrol", "Patrol", 24, 24, NativeFormat: !legacy));
        var areas = Enumerable.Range(1, 8).Select(id => document.AddMapRegion(MapRegionKind.Area, new(id * 2, id * 2, 2, 2))).ToArray();
        var owner = document.ScriptOwners().Single(item => item.Scope == 4 && item.Record.File == areas[0].File && item.Record.Offset == areas[0].Offset);
        var root = document.EnsureScriptScope(owner);
        document.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED"));
        var NextName = legacy ? "nextid" : "bAreaNextID";
        var WaitName = legacy ? "waitdelay" : "wAreaWaitDelay";
        PatrolRoute Route(int id = 1) => document.PatrolRouteFrom(areas[id - 1]);
        void Links(params (int From, int To)[] edges) => document.Edit("Patrol fixture", () =>
        {
            for (var index = 0; index < areas.Length; index++)
            {
                var to = edges.FirstOrDefault(edge => edge.From == index + 1).To;
                document.SetField(areas[index], areas[index].Fields.Single(field => field.Name == NextName), to.ToString());
            }
        });
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> expected)
        { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(expected[file]), "Patrol operation changed unrelated buffers, coordinates, scenarios or properties."); }
        void Check(Action edit, (int From, int To)[] links, (int Area, int Delay)? pause = null)
        {
            var before = Snapshot(); var expected = before.ToDictionary(pair => pair.Key, pair => (byte[])pair.Value.Clone());
            void Encode(int id, string name, int value)
            {
                var record = areas[id - 1]; var field = record.Fields.Single(item => item.Name == name);
                field.Encode(value.ToString()).CopyTo(expected[record.File], record.Offset + field.Offset);
            }
            foreach (var edge in links) Encode(edge.From, NextName, edge.To);
            if (pause is { } value) Encode(value.Area, WaitName, value.Delay);
            var changes = 0; EventHandler handler = (_, _) => changes++; document.Changed += handler;
            try { edit(); Exact(expected); Expect(changes == 1, "Patrol edit was not one transaction."); }
            finally { document.Changed -= handler; }
            document.Undo(); Exact(before); document.Redo(); Exact(expected);
        }
        Links((1, 2), (2, 3));
        var unchanged = Snapshot(); var undo = document.CanUndo; var redo = document.CanRedo;
        var paths = document.PatrolRoutes(); Expect(paths.Count == 1 && paths[0].Points.Select(point => point.ID).SequenceEqual(new uint[] { 1, 2, 3 }) && paths[0].EndID == 0, "Open patrol projection is incorrect.");
        Exact(unchanged); Expect(undo == document.CanUndo && redo == document.CanRedo, "Viewing routes changed history.");
        Check(() => document.AppendPatrolPoint(Route(), areas[3], 17), [(3, 4)], (4, 17));
        Check(() => document.InsertPatrolPoint(Route(), 1, areas[4], 23), [(1, 5), (5, 2)], (5, 23));
        Expect(Route().Points.Select(point => point.ID).SequenceEqual(new uint[] { 1, 5, 2, 3, 4 }), "Inserted patrol node order is incorrect.");
        Check(() => document.RemovePatrolOccurrence(Route(), 1), [(1, 2), (5, 0)]);
        Check(() => document.AppendPatrolPoint(Route(), areas[1], 29), [(4, 2)], (2, 29));
        Expect(Route().Closed && Route().EndID == 2 && Route().OccurrenceCount == 5, "A partial loop was rejected or lost its distinct closing occurrence.");
        Check(() => document.RemovePatrolOccurrence(Route(), 4), [(4, 0)]); // Open loop, preserve area 2's outgoing link.
        Check(() => document.LinkPatrolAreas(areas[3], areas[0]), [(4, 1)]);
        Check(() => document.InsertPatrolPoint(Route(), Route().Points.Count, areas[5], 31), [(4, 6), (6, 1)], (6, 31));
        Check(() => document.RemovePatrolOccurrence(Route(), 0), [(1, 0)]); // [2,3,4,6,1] remains open.
        Expect(Route(2).Points.Select(point => point.ID).SequenceEqual(new uint[] { 2, 3, 4, 6, 1 }), "Removing first closed-route occurrence did not retain the ending area.");
        Links((1, 2), (2, 3), (4, 2));
        Expect(document.PatrolRoutes().Count == 2 && document.PatrolRoutes().All(route => route.Points.Any(point => point.ID == 2)), "Joined routes lost their shared tail.");
        Check(() => document.RemovePatrolOccurrence(Route(), 1), [(1, 3), (2, 0)]);
        Expect(Route(4).Points.Select(point => point.ID).SequenceEqual(new uint[] { 4, 2 }), "Source-style selected-path deletion silently rewired another incoming edge.");
        Links((1, 2), (2, 3));
        Check(() => document.ClearPatrolRoute(Route()), [(1, 0), (2, 0)]);
        Expect(document.PatrolRoutes().Count == 0 && document.PatrolAreas().Count == 8, "Deleting a route deleted areas or invented persistent one-point paths.");
        Check(() => document.LinkPatrolAreas(areas[0], areas[0]), [(1, 1)]);
        Expect(Route().Closed && Route().Points.Count == 1 && Route().OccurrenceCount == 2, "Self patrol loop was lost.");
        Check(() => document.RemovePatrolOccurrence(Route(), 1), [(1, 0)]);
        Links((1, 2), (2, 3), (3, 2));
        Check(() => document.InsertPatrolPoint(Route(), 0, areas[6], 12), [(7, 1)], (7, 12));
        Expect(Route(7).Points.Select(point => point.ID).SequenceEqual(new uint[] { 7, 1, 2, 3 }) && Route(7).EndID == 2, "Insertion before first point lost the existing partial loop.");
        Check(() => document.RemovePatrolOccurrence(Route(7), 1), [(7, 2), (1, 0)]);
        Check(() => document.ClearPatrolRoute(Route(7)), [(7, 0), (2, 0), (3, 0)]);
        Links((1, 2), (2, 3));
        Check(() => document.RemovePatrolOccurrence(Route(), 2), [(2, 0)]);
        Links();
        Check(() => document.SetPatrolPause(areas[0], 250), [], (1, 250));
        document.Undo(); unchanged = Snapshot();
        var oldDelay = (int)document.PatrolAreas()[0].WaitDelay; document.SetPatrolPause(areas[0], oldDelay); Exact(unchanged);
        Expect(document.CanRedo, "No-op patrol pause consumed redo.");
        foreach (var delay in new[] { -1, 251, int.MaxValue })
        {
            try { document.SetPatrolPause(areas[0], delay); throw new Exception("Invalid source pause was accepted."); } catch (ArgumentException) { }
            Exact(unchanged); Expect(document.CanRedo, "Rejected patrol pause consumed redo.");
        }
        Links((1, 2), (2, 3)); var stale = Route(); Links((1, 3)); unchanged = Snapshot();
        try { document.AppendPatrolPoint(stale, areas[3], 0); throw new Exception("Stale route was edited."); } catch (InvalidOperationException) { } Exact(unchanged);
        try { document.LinkPatrolAreas(areas[0], areas[3]); throw new Exception("Linked quick-draw endpoint was overwritten."); } catch (InvalidOperationException) { } Exact(unchanged);
        try { document.InsertPatrolPoint(Route(), 1, areas[0], 0); throw new Exception("Linked area was inserted."); } catch (InvalidOperationException) { } Exact(unchanged);
        var foreign = NativeMapDocument.Create(24, 24, "Foreign").AddMapRegion(MapRegionKind.Area, new(1, 1, 1, 1));
        try { document.AppendPatrolPoint(Route(), foreign, 0); throw new Exception("Foreign patrol area was accepted."); } catch (ArgumentException) { } Exact(unchanged);
        Links((1, 222)); Expect(Route().EndID == 222 && !Route().Closed, "Missing patrol target was guessed or hidden.");
        Check(() => document.RemovePatrolOccurrence(Route(), 1), [(1, 0)]);
        var selection = new TileRect(2, 2, 2, 2); Expect(document.FindPatrolArea(selection)?.Offset == areas[0].Offset, "Quick path exact-area match failed.");
        document.LinkPatrolAreas(areas[0], areas[1]); Expect(document.FindPatrolArea(selection, true) is null, "Quick new path selected an already linked start.");
        var larger = document.AddMapRegion(MapRegionKind.Area, new(1, 1, 6, 6));
        Expect(document.FindPatrolArea(new(3, 3, 1, 1))?.Offset == areas[0].Offset, "Quick-path dimension preference does not follow original candidate ordering.");
        // Exhaust all occurrence positions for lengths 1–8, open endings and
        // every possible cycle entry: 276 cases per format, 552 total.
        for (var length = 1; length <= 8; length++)
        for (var cycle = 0; cycle <= length; cycle++)
        for (var occurrence = 0; occurrence < length + (cycle == 0 ? 0 : 1); occurrence++)
        {
            var edges = Enumerable.Range(1, length).Select(id => (From: id, To: id == length ? cycle : id + 1)).ToArray();
            Links(edges); var before = Snapshot(); var expected = before.ToDictionary(pair => pair.Key, pair => (byte[])pair.Value.Clone());
            var sequence = Enumerable.Range(1, length).Concat(cycle == 0 ? [] : new[] { cycle }).ToArray();
            var remaining = sequence.Where((_, index) => index != occurrence).ToArray();
            var graph = edges.ToDictionary(edge => edge.From, edge => edge.To);
            if (occurrence > 0) graph[sequence[occurrence - 1]] = occurrence + 1 < sequence.Length ? sequence[occurrence + 1] : 0;
            var removed = sequence[occurrence];
            if (!remaining.Contains(removed) || remaining[^1] == removed) graph[removed] = 0;
            foreach (var (id, target) in graph)
            {
                var record = areas[id - 1]; var field = record.Fields.Single(item => item.Name == NextName);
                field.Encode(target.ToString()).CopyTo(expected[record.File], record.Offset + field.Offset);
            }
            var notifications = 0; EventHandler handler = (_, _) => notifications++; document.Changed += handler;
            try { document.RemovePatrolOccurrence(Route(), occurrence); Exact(expected); }
            finally { document.Changed -= handler; }
            var modified = before.Any(pair => !pair.Value.AsSpan().SequenceEqual(expected[pair.Key]));
            Expect(notifications == (modified ? 1 : 0), "Exhaustive path removal notified an incorrect number of transactions.");
            if (modified) { document.Undo(); Exact(before); document.Redo(); Exact(expected); }
        }
        Links((1, 2), (2, 3), (3, 2), (4, 2));
        var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "patrol-" + legacy)); var reopened = NativeMapDocument.Load(saved);
        Expect(reopened.PatrolAreas().Select(point => (point.ID, point.NextID, point.WaitDelay)).SequenceEqual(document.PatrolAreas().Select(point => (point.ID, point.NextID, point.WaitDelay))), "Patrol save/reopen lost links or delays.");
        if (legacy)
        {
            var compiled = document.CompileLegacyProject("PatrolGame", NativeMapDocument.Create(24, 24, "Basis"));
            Expect(compiled.PatrolAreas().Select(point => (point.ID, point.NextID, point.WaitDelay)).SequenceEqual(document.PatrolAreas().Select(point => (point.ID, point.NextID, point.WaitDelay))), "Compiled patrol links/pauses differ from source project.");
        }
        // Deliberately malformed imported identities, bypassing the normal ID
        // editor's collision guard only in private test exports.
        var damagedRoot = Path.Combine(fixture.DirectoryPath, "patrol-duplicate-" + legacy);
        var damagedPath = SaveProjectionChecks.SaveCopy(document, damagedRoot);
        var second = areas[1]; var idField = second.Fields.Single(field => field.Name == (legacy ? "id" : "bAreaID"));
        if (legacy)
        {
            var projectFile = LegacySectionFile.Read(damagedPath);
            File.WriteAllBytes(damagedPath, LegacySectionFile.Create(projectFile.Sections.Select(section =>
            {
                if (section.Signature != NativeMapDocument.LegacyAreas) return section;
                var payload = section.Payload.ToArray(); idField.Encode("1").CopyTo(payload, second.Offset - 12 + idField.Offset);
                return new LegacySection(section.Signature, section.Version, payload);
            })).ToArray());
        }
        else
        {
            var thingsPath = Path.Combine(damagedRoot, second.File.ExportPath); var bytes = File.ReadAllBytes(thingsPath);
            idField.Encode("1").CopyTo(bytes, second.Offset + idField.Offset); File.WriteAllBytes(thingsPath, bytes);
        }
        var damaged = NativeMapDocument.Load(damagedPath); var damagedBytes = damaged.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var brokenRoute = damaged.PatrolRoutes().First(route => route.Ambiguous);
        try { damaged.ClearPatrolRoute(brokenRoute); throw new Exception("Ambiguous imported patrol identities were silently edited."); } catch (InvalidOperationException) { }
        try { damaged.SetPatrolPause(brokenRoute.Points[0].Record, 10); throw new Exception("Ambiguous patrol pause owner was silently chosen."); } catch (InvalidOperationException) { }
        foreach (var file in damaged.Files) Expect(file.Data.Span.SequenceEqual(damagedBytes[file]), "Malformed patrol inspection/rejection changed source buffers.");
        Expect(!damaged.CanUndo && !damaged.CanRedo && !damaged.HasChanges, "Malformed patrol rejection changed history/dirty state.");
    }
}

void TestMapRegions()
{
    using var fixture = new TemporaryFixture();
    foreach (var legacy in new[] { false, true })
    {
        var document = NativeMapDocument.CreateNewMap(new("Regions", "Regions", 16, 16, NativeFormat: !legacy));
        Dictionary<NativeFile, byte[]> Snapshot() => document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(Dictionary<NativeFile, byte[]> expected)
        { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(expected[file]), "Region operation changed unexpected bytes."); }
        string Read(NativeRecord record, string name) => record.Read(record.Fields.Single(field => field.Name == name));
        void Set(NativeRecord record, string name, string value) => document.SetField(record, record.Fields.Single(field => field.Name == name), value);
        var baseline = Snapshot(); var changed = 0; document.Changed += (_, _) => changed++;
        var records = new Dictionary<MapRegionKind, NativeRecord>();
        foreach (var kind in Enum.GetValues<MapRegionKind>())
        {
            var before = Snapshot(); var notifications = changed;
            var initial = kind == MapRegionKind.Gate ? new TileRect(0, 0, 1, 1) : new(0, 0, 16, 16);
            var record = document.AddMapRegion(kind, initial); records.Add(kind, record);
            Expect(changed == notifications + 1 && document.GetMapRegion(record) is { } region && region.Kind == kind && region.Bounds == initial, "Region creation is not one atomic source-shaped edit.");
            var after = Snapshot(); document.Undo(); Exact(before); document.Redo(); Exact(after);
            var id = legacy ? "id" : kind == MapRegionKind.Area ? "bAreaID" : kind == MapRegionKind.Gate ? "bGateID" : "bEffectID";
            Expect(Read(record, id) == "1", "Area, gate and effect must allocate independently; native hero gate ID is constant one.");
            if (legacy)
            {
                Set(record, "name", "Region " + kind); Set(record, "flags", "5");
                Set(record, "rect.color", "1193046"); Set(record, "rect.textcolor", "11259375");
            }
            if (kind == MapRegionKind.Effect)
            { Set(record, legacy ? "effect1" : "wEffect1", "17"); Set(record, legacy ? "freq" : "bEffectFrequency", "19"); }
        }
        var area = records[MapRegionKind.Area];
        var next = document.AddMapRegion(MapRegionKind.Area, new(10, 10, 2, 2));
        Set(area, legacy ? "nextid" : "bAreaNextID", "2"); Set(area, legacy ? "waitdelay" : "wAreaWaitDelay", "71");
        var owner = document.ScriptOwners().Single(item => item.Scope == 4 && item.Record.File == area.File && item.Record.Offset == area.Offset);
        var root = document.EnsureScriptScope(owner);
        var timer = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
        document.InsertScriptEvent(root.Owner, root.Field, timer);
        foreach (var (kind, record) in records)
        {
            var bounds = kind == MapRegionKind.Gate ? new TileRect(15, 15, 1, 1) : new(3, 4, 5, 6);
            var before = Snapshot(); var expected = before.ToDictionary(pair => pair.Key, pair => (byte[])pair.Value.Clone());
            var fields = legacy ? new[] { ("rect.x1", bounds.X), ("rect.y1", bounds.Y), ("rect.x2", bounds.X + bounds.Width - 1), ("rect.y2", bounds.Y + bounds.Height - 1) }
                : kind switch {
                    MapRegionKind.Area => new[] { ("dwAreaLocationStartX", bounds.X), ("dwAreaLocationStartY", bounds.Y), ("dwAreaLocationEndX", bounds.X + bounds.Width - 1), ("dwAreaLocationEndY", bounds.Y + bounds.Height - 1) },
                    MapRegionKind.Effect => new[] { ("dwEffectStartX", bounds.X), ("dwEffectStartY", bounds.Y), ("wEffectWidth", bounds.Width - 1), ("wEffectHeight", bounds.Height - 1) },
                    _ => new[] { ("dwGateLocationX", bounds.X), ("dwGateLocationY", bounds.Y) } };
            foreach (var (name, value) in fields)
            {
                var field = record.Fields.Single(item => item.Name == name);
                field.Encode(value.ToString()).CopyTo(expected[record.File], record.Offset + field.Offset);
            }
            var notifications = changed;
            document.SetMapRegionBounds(record, bounds, record.File.Data.Slice(record.Offset, record.Length));
            Exact(expected); Expect(changed == notifications + 1 && document.GetMapRegion(record)!.Bounds == bounds, "Region coordinates/notification differ from exact independent encoding.");
            document.Undo(); Exact(before); document.Redo(); Exact(expected);
            document.SetMapRegionBounds(record, new(14, 15, 1, 1));
            if (!legacy && kind == MapRegionKind.Effect) Expect(Read(record, "wEffectWidth") == "0" && Read(record, "wEffectHeight") == "0", "Single-tile effect must encode zero deltas.");
            document.Undo(); Exact(expected);
            // A no-op and all rejected operations must retain redo and the complete graph/opaque bytes.
            notifications = changed; document.SetMapRegionBounds(record, bounds); Exact(expected);
            Expect(document.CanRedo && changed == notifications, "No-op region bounds consumed redo or notified mutation.");
            foreach (var invalid in new[] { new TileRect(-1, 0, 1, 1), new(0, 0, 0, 1), new(15, 15, 2, 1), new(1, 1, int.MaxValue, 1) })
            {
                try { document.SetMapRegionBounds(record, invalid); throw new Exception("Invalid region bounds were accepted."); } catch (ArgumentException) { }
                Exact(expected); Expect(document.CanRedo && changed == notifications, "Rejected region bounds consumed redo or changed history.");
            }
            if (kind == MapRegionKind.Gate)
                try { document.SetMapRegionBounds(record, new(1, 1, 2, 2)); throw new Exception("A gate rectangle was accepted."); } catch (ArgumentException) { }
            var stale = record.File.Data.Slice(record.Offset, record.Length).ToArray(); stale[^1] ^= 1;
            try { document.SetMapRegionBounds(record, new(1, 1, 1, 1), stale); throw new Exception("Stale region payload was accepted."); } catch (InvalidOperationException) { }
            Exact(expected);
        }
        var foreign = NativeMapDocument.Create(16, 16, "Foreign").AddMapRegion(MapRegionKind.Area, new(1, 1, 1, 1));
        var stable = Snapshot(); try { document.SetMapRegionBounds(foreign, new(1, 1, 1, 1)); throw new Exception("Foreign region was accepted."); } catch (ArgumentException) { } Exact(stable);
        // Rendering normalizes reversed endpoints without changing source bytes.
        Set(area, legacy ? "rect.x1" : "dwAreaLocationStartX", "9"); Set(area, legacy ? "rect.x2" : "dwAreaLocationEndX", "2");
        stable = Snapshot(); Expect(document.GetMapRegion(area)!.Bounds == new TileRect(2, 4, 8, 6), "Reversed endpoints are not displayed as the inclusive source rectangle."); Exact(stable);
        document.SetMapRegionBounds(area, new(3, 4, 5, 6));
        // Explicit bounds can repair an otherwise unrenderable verified record; no automatic coercion.
        Set(area, legacy ? "rect.x1" : "dwAreaLocationStartX", legacy ? int.MinValue.ToString() : uint.MaxValue.ToString());
        stable = Snapshot(); Expect(document.GetMapRegion(area) is null, "Overflowing imported region was silently coerced."); Exact(stable);
        document.SetMapRegionBounds(area, new(3, 4, 5, 6)); document.Undo(); Exact(stable); document.Redo();
        if (legacy)
        {
            Set(area, "node.type", "99"); stable = Snapshot();
            Expect(document.GetMapRegion(area) is null, "Unknown rectangle kind was guessed as an area.");
            try { document.SetMapRegionBounds(area, new(1, 1, 1, 1)); throw new Exception("Unknown rectangle kind was editable through geometry."); } catch (ArgumentException) { }
            Exact(stable); document.Undo();
        }
        var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "regions-" + legacy));
        var reopened = NativeMapDocument.Load(saved); var savedBytes = document.Files.ToDictionary(file => file.ExportPath, file => file.Data.ToArray());
        // A project's MainFile is the immutable loaded container; edited section
        // buffers are assembled only on export. Compare all authoritative buffers.
        if(legacy)SaveProjectionChecks.RoundTrip(document,reopened,savedBytes);
        else foreach(var file in reopened.Files)Expect(file.Data.Span.SequenceEqual(savedBytes[file.ExportPath]),"Native region save/reopen altered data: "+file.ExportPath);
        if (legacy)
        {
            var repeated = SaveProjectionChecks.SaveCopy(reopened, Path.Combine(fixture.DirectoryPath, "regions-repeated"));
            Expect(SaveProjectionChecks.BeforeEquals(reopened,File.ReadAllBytes(saved)), "Reopened region project did not export the exact serialized container.");
        }
        Expect(reopened.MapRegions().Select(item => (item.Kind, item.Bounds)).SequenceEqual(document.MapRegions().Select(item => (item.Kind, item.Bounds))), "Region geometry changed after reopen.");
        if (legacy)
        {
            var compiled = document.CompileLegacyProject("RegionsGame", NativeMapDocument.Create(16, 16, "Basis"));
            Expect(compiled.MapRegions().Select(item => (item.Kind, item.Bounds)).OrderBy(item => item.Kind).SequenceEqual(document.MapRegions().Select(item => (item.Kind, item.Bounds)).OrderBy(item => item.Kind)), "Compiled area/effect endpoints or gate points are incorrect.");
            var compiledArea = compiled.MapRegions().First(item => item.Kind == MapRegionKind.Area).Record;
            Expect(Read(compiledArea, "bAreaNextID") == "2" && Read(compiledArea, "wAreaWaitDelay") == "71" && Read(compiledArea, "wAreaTrigger") != "0", "Geometry editing lost patrol/scenario data during compilation.");
            foreach (var kind in new[] { MapRegionKind.Gate, MapRegionKind.Effect })
            {
                var duplicate = document.DuplicateRecord(records[kind]);
                Expect(Read(duplicate, "id") == "2" && document.GetMapRegion(duplicate)!.Kind == kind && Read(duplicate, "name") == "Region " + kind, "Duplication used another rectangle category's ID namespace or lost name.");
                document.Undo();
            }
            var areaCopy = document.DuplicateRecord(area);
            Expect(Read(areaCopy, "id") == "3" && document.GetMapRegion(areaCopy)!.Bounds == document.GetMapRegion(area)!.Bounds, "Area duplication did not allocate in the occupied area namespace.");
            var copiedOwner = document.ScriptOwners().Single(item => item.Scope == 4 && item.Record.File == areaCopy.File && item.Record.Offset == areaCopy.Offset);
            Expect(document.FindScriptScope(copiedOwner) is { Target: not 0 } copiedRoot && copiedRoot.Target != document.FindScriptScope(owner)!.Target, "Area duplication did not clone its owned scenario independently.");
            document.Undo();
            var exhausted = NativeMapDocument.CreateNewMap(new("FullIDs", "FullIDs", 16, 16));
            exhausted.Edit("Fill area IDs", () => { for (var index = 1; index <= 255; index++) exhausted.AddMapRegion(MapRegionKind.Area, new(1, 1, 1, 1)); });
            var exhaustionBytes = exhausted.Files.ToDictionary(file => file, file => file.Data.ToArray());
            try { exhausted.AddMapRegion(MapRegionKind.Area, new(1, 1, 1, 1)); throw new Exception("Exhausted byte IDs were accepted."); } catch (InvalidOperationException) { }
            foreach (var file in exhausted.Files) Expect(file.Data.Span.SequenceEqual(exhaustionBytes[file]), "ID exhaustion partially appended a rectangle.");
            Expect(Read(exhausted.AddMapRegion(MapRegionKind.Gate, new(1, 1, 1, 1)), "id") == "1", "Full area namespace blocked independent gates.");
        }
        else
        {
            var gateCopy = document.DuplicateRecord(records[MapRegionKind.Gate]);
            Expect(Read(gateCopy, "bGateID") == "1", "Native gate ID was incorrectly treated as a unique region identity.");
            document.Undo();
        }
        // Original compiler neighbour rule, including indestructible boundary and unknown variants.
        foreach (var variant in new byte[] { 1, 2, 99 })
        foreach (var solid in new byte[] { 4, 1, 2, 6, 7, 9, 30, 254 })
        {
            document.Edit("Gate fixture", () => {
                document.Paint([new(7, 7)], new(37, 1, variant)); document.Paint([new(7, 8), new(8, 7)], new(solid, 2, 1));
            });
            var gate = document.AddMapRegion(MapRegionKind.Gate, new(7, 7, 1, 1));
            var nativeGate = legacy ? document.CompileLegacyProject("GateInfo", NativeMapDocument.Create(16, 16, "Basis")).MapRegions().Last(item => item.Kind == MapRegionKind.Gate).Record : gate;
            Expect(Read(nativeGate, "bGateID") == "1" && Read(nativeGate, "wGateHealth") == "100" && Read(nativeGate, "bGlobalPlayer") == "1", "Created hero gate lost original defaults.");
            Expect(Read(nativeGate, "bGateType") == (variant == 99 ? "33" : "37") && Read(nativeGate, "bGateOrientation") == (variant == 99 ? 0 : variant == 1 ? solid == 4 ? 4 : 0 : solid == 4 ? 2 : 6).ToString(), "Hero gate type/orientation differs from source neighbour rules.");
            document.Undo(); document.Undo();
        }
        foreach (var file in document.Files.Where(file => file == document.TileFile)) Expect(file.Data.Span.SequenceEqual(baseline[file]), "Geometry editing modified terrain.");
    }
}

void TestNewMapCreation()
{
    using var fixture = new TemporaryFixture();
    var presets = LegacyCompilerRules.Elements("NewMap"); Expect(presets.Count == 23, "The original new-map background catalogue is incomplete.");
    var sequence = 0;
    foreach (var preset in presets)
    foreach (var version in new byte[] { 13, 16, 17 })
    foreach (var native in new[] { false, true })
    foreach (var starter in new[] { false, true })
    {
        var options = new NewMapOptions("NewFixture", native ? "Новая карта" : "New display €", 32, 20, sequence++ % 5 + 1, version, preset.ID, starter, native);
        var document = NativeMapDocument.CreateNewMap(options);
        Expect(document.IsLegacyProject != native && document.Width == 32 && document.Height == 20 && document.HasChanges && !document.CanUndo && !document.CanRedo, "New map format/dimensions/unsaved history are incorrect.");
        var fill = new MapTile((byte)(preset.ID >> 24), (byte)(preset.ID >> 16), (byte)(preset.ID >> 8));
        for (var y = 0; y < 20; y++) for (var x = 0; x < 32; x++)
        {
            var expected = x == 0 || y == 0 || x == 31 || y == 19 ? new MapTile(30, 2, 1) : fill;
            for (var keeper = 0; starter && keeper < options.Players; keeper++)
                if (x >= 1 + keeper * 6 && x < 6 + keeper * 6 && y is >= 1 and <= 5) expected = new(14, (byte)(keeper + 3), 1);
            Expect(document.GetTile(x, y) == expected, "New map preset/border/starter bytes differ from independent expected tiles.");
        }
        var terrain = TerrainLayoutDiagnostics.Validate(document);
        Expect(starter ? terrain.Count == 0 : terrain.Count == 1 && terrain[0].Code == "DK2N023", "Blank map silently acquired hearts or starter layout failed validation.");
        var records = document.Records();
        string Read(NativeRecord record, string field) => record.Read(record.Fields.Single(item => item.Name == field));
        if (!native)
        {
            var info = records.Single(record => record.Category == "Project.MAPPROPERTIES");
            Expect(Read(info, "Name") == options.DisplayName && Read(info, "Version") == version.ToString() && Read(info, "Players") == options.Players.ToString()
                && Read(info, "FillID") == unchecked((int)preset.ID).ToString() && Read(info, "Options") == "4" && Read(info, "Options2") == "0" && Read(info, "GameTicks") == "4" && Read(info, "Soundtrack") == "1", "New-map metadata defaults do not match Properties_MapResetOptions.");
            for (var left = 0; left < 7; left++) for (var right = 0; right < 7; right++)
                Expect(Read(info, $"Allies[{left}][{right}]") == ((left == 1) != (right == 1) ? "1" : "0"), "Neutral alliance defaults are incorrect.");
            foreach (var item in LegacyCompilerRules.Elements("MapVars").Where(item => item.ID < 256))
                Expect(Read(info, $"Variables[{item.ID}]") == unchecked((int)item.Variable).ToString(), "New map lost original variable defaults.");
            var players = records.Where(record => record.Category == "Project.MAPOPTIONS").ToArray(); Expect(players.Length == 7, "A project must retain all seven player option rows.");
            for (var player = 1; player <= 7; player++)
            {
                var row = players[player - 1];
                Expect(Read(row, "PlayerHealth") == "100" && Read(row, "PlayerGold") == "3000" && Read(row, "PlayerComputer") == (player <= 2 ? "1" : "0"), "New player health/gold/computer defaults are incorrect.");
                foreach (var rule in LegacyCompilerRules.DefaultPlayerAI) Expect(Read(row, rule.Target) == rule.Source, "A source AI default was not applied to a new player.");
                foreach (var name in new[] { "PlayerCPUAreaX1", "PlayerCPUAreaY1", "PlayerCPUAreaX2", "PlayerCPUAreaY2", "PlayerCameraX", "PlayerCameraY" }) Expect(Read(row, name) == "-1", "New project lost automatic camera/build-area sentinel.");
                for (var index = 0; index < 45; index++)
                {
                    var count = player <= 2 ? -1 : index is 13 or 14 ? 20 : index is >= 1 and <= 12 && index is not (6 or 11) ? 32 : -1;
                    Expect(Read(row, $"CreaturesCount[{index}]") == count.ToString(), "New-map creature pools differ from original defaults (not ASCII-import defaults).");
                }
                foreach (var (array, count, available) in new[] { ("Rooms", 15, 15), ("Traps", 14, 11), ("Doors", 8, 6), ("Spells", 12, 12) })
                    for (var index = 0; index < count; index++)
                        Expect(Read(row, $"{array}[{index}]") == (player <= 2 ? "0" : index >= available ? "255" : array == "Spells" && index is >= 2 and <= 10 ? "2" : "3"), "New-map availability defaults are incorrect.");
            }
            var initial = NewMapOptions.ForDocument(document);
            Expect(initial.DisplayName == options.DisplayName && initial.FillID == preset.ID && initial.Version == version && initial.Players == options.Players && !initial.PlaceHearts, "A new dialog cannot reuse original current-map settings without reusing its content.");
            var compiled = document.CompileLegacyProject("CompileNew", NativeMapDocument.Create(32, 20, "Basis"));
            Expect(compiled.Records().Count(record => record.Category == "GAMEPLAYER") == (starter ? options.Players + 2 : 2), "Normal compilation unexpectedly included heartless keepers; factory mode leaked into compilation.");
        }
        else
        {
            Expect(Read(records.Single(record => record.Category == "Map"), "wstrMainName") == options.DisplayName, "Unicode display name was written at the wrong native offset.");
            var players = records.Where(record => record.Category == "GAMEPLAYER").ToArray();
            Expect(players.Select(row => Read(row, "bGlobalPlayer")).SequenceEqual(Enumerable.Range(1, options.Players + 2).Select(value => value.ToString())), "Native factory must expose each requested keeper, even on an unfinished blank map.");
            foreach (var row in players)
            {
                var owner = int.Parse(Read(row, "bGlobalPlayer"));
                Expect(Read(row, "bPlayerGold") == "3000", "Native player lost source gold default.");
                if (owner >= 3)
                    Expect(Read(row, "bPlayerCameraX") == (starter ? 3 + (owner - 3) * 6 : 16).ToString() && Read(row, "bPlayerCameraY") == (starter ? 3 : 10).ToString(), "Native camera is neither the heart centre nor the blank-map fallback.");
            }
            Expect(records.Any(record => record.Category == "GAMEVARCREATURECOUNT") && records.Any(record => record.Category == "GAMEVARTHINGSTATUS"), "New native blank map lost keeper pool/availability defaults.");
        }
        var before = document.Files.ToDictionary(file => file.ExportPath, file => file.Data.ToArray());
        document.Paint(new[] { new TilePoint(30, 18) }, new(99, 7, 2, 211)); document.Undo();
        foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file.ExportPath]), "First edit undo did not restore the new document exactly.");
        if (sequence % 23 == 0)
        {
            var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "new-" + sequence));
            var reopened = NativeMapDocument.Load(saved);
            if(document.IsLegacyProject)SaveProjectionChecks.RoundTrip(document,reopened,before);
            else foreach(var file in reopened.Files)Expect(file.Data.Span.SequenceEqual(before[file.ExportPath]),"Native new map save/reopen lost defaults/terrain/opaque bytes.");
        }
    }
    var invalid = new (NewMapOptions Options, NewMapProblem Problem)[] {
        (new(FileName: "../escape"), NewMapProblem.FileName), (new(FileName: "CON"), NewMapProblem.FileName), (new(FileName: "NUL.txt"), NewMapProblem.FileName),
        (new(DisplayName: "Русский DKM"), NewMapProblem.DisplayName), (new(DisplayName: new string('x', 64)), NewMapProblem.DisplayName), (new(DisplayName: "x\0x"), NewMapProblem.DisplayName),
        (new(Width: 6), NewMapProblem.Dimensions), (new(Height: 1025), NewMapProblem.Dimensions), (new(Players: 0), NewMapProblem.Players), (new(Players: 6), NewMapProblem.Players),
        (new(Version: 15), NewMapProblem.Version), (new(FillID: 0x0E030101), NewMapProblem.Fill), (new(Width: 7, Height: 7, Players: 2, PlaceHearts: true), NewMapProblem.StarterSpace) };
    foreach (var (options, problem) in invalid)
    {
        Expect(options.Validate() == problem, "New map error classification is incorrect.");
        try { _ = NativeMapDocument.CreateNewMap(options); throw new InvalidOperationException("Invalid map factory settings were accepted."); }
        catch (ArgumentException) { }
    }
    Expect(!new NewMapOptions(Width: 120, Height: 120).NeedsLargeMapWarning && new NewMapOptions(Width: 121, Height: 120).NeedsLargeMapWarning
        && new NewMapOptions(Width: 301, Height: 7).NeedsLargeMapWarning, "Large-map warning does not cover original area threshold and extended dimension range.");
    Expect(NativeMapDocument.CreateNewMap(new(Width: 7, Height: 7, PlaceHearts: true)).GetTile(5, 5).Terrain == 14, "The smallest original map cannot contain its single heart.");
}

void TestTerrainDiagnostics()
{
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "diagnostics.dkm");
    var input = TextMapFile.Parse("GUI_GENERATED_MAP\nDiagnostics\nPLAYERS 1\nMAP\n32 X 32\n" + string.Concat(Enumerable.Repeat(string.Concat(Enumerable.Repeat("Oa", 32)) + "\n", 32)) + "EOF").ToArray();
    File.WriteAllBytes(path, input);
    (int Terrain, int Variant, int MinW, int MinH, int MaxW, int MaxH)[] rules = { (14, 1, 5, 5, 5, 5),
        (12, 1, 3, 3, 5, 5), (40, 1, 3, 3, 5, 5), (28, 1, 3, 1, 3, 0), (29, 1, 1, 1, 0, 0),
        (33, 1, 2, 2, 2, 2), (34, 1, 3, 2, 3, 0), (37, 1, 3, 1, 3, 1), (37, 2, 1, 3, 1, 3) };
    foreach (var legacy in new[] { false, true })
    {
        var document = legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(32, 32, "Diagnostics");
        document.Paint(MapDrawing.Rectangle(new(0, 0, 32, 32), true), new(2, 2, 1, 211));
        // Deliberately keep both an undo entry and a redo entry throughout checks.
        document.Paint(new[] { new TilePoint(30, 30) }, new(4, 2, 1, 217)); document.Undo();
        void Check(params string[] codes)
        {
            var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
            var undo = document.CanUndo; var redo = document.CanRedo; var changed = 0;
            EventHandler handler = (_, _) => changed++; document.Changed += handler;
            try
            {
                var diagnostics = TerrainLayoutDiagnostics.Validate(document);
                Expect(diagnostics.Select(item => item.Code).Order().SequenceEqual(codes.Order()), "Unexpected terrain diagnostics: " + string.Join(", ", diagnostics.Select(item => item.Code)));
                var structural = NativeMapValidator.Validate(document);
                var full = NativeMapValidator.Validate(document, includeGameplay: true);
                Expect(full.SequenceEqual(structural.Concat(diagnostics)), "Full validation did not include the exact read-only gameplay results.");
                Expect(changed == 0 && undo == document.CanUndo && redo == document.CanRedo, "Validation changed notifications or history.");
                foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Validation changed bytes: " + file.ExportPath);
            }
            finally { document.Changed -= handler; }
        }
        void Paint(int x, int y, int w, int h, byte terrain, byte owner = 3, byte variant = 1) =>
            document.Paint(MapDrawing.Rectangle(new(x, y, w, h), true), new(terrain, owner, variant, 231));
        Check("DK2N023"); Paint(1, 1, 5, 5, 14); Check();
        foreach (var rule in rules.Where(rule => rule.Terrain != 14))
        {
            Paint(12, 10, rule.MinW, rule.MinH, (byte)rule.Terrain, 1, (byte)rule.Variant); Check(); document.Undo();
            // Check either a too-narrow rectangle or a too-short vertical gate.
            if (rule.MinW > 1 || rule.MinH > 1)
            {
                Paint(12, 10, Math.Max(1, rule.MinW - 1), Math.Max(1, rule.MinH - 1), (byte)rule.Terrain, 1, (byte)rule.Variant); Check("DK2N021");
                Expect(TerrainLayoutDiagnostics.Validate(document).Single().Severity == DiagnosticSeverity.Warning, "Bad portal/gate dimensions should warn, not be fatal."); document.Undo();
            }
            if (rule.MaxW != 0)
            {
                Paint(12, 10, rule.MaxW + 1, rule.MinH, (byte)rule.Terrain, 1, (byte)rule.Variant); Check("DK2N021"); document.Undo();
            }
            if (rule.MaxH != 0)
            {
                Paint(12, 10, rule.MinW, rule.MaxH + 1, (byte)rule.Terrain, 1, (byte)rule.Variant); Check("DK2N021"); document.Undo();
            }
            if (rule.MaxW == 0 || rule.MaxH == 0)
            {
                Paint(12, 10, rule.MaxW == 0 ? 9 : rule.MinW, rule.MaxH == 0 ? 12 : rule.MinH, (byte)rule.Terrain, 1, (byte)rule.Variant); Check(); document.Undo();
            }
        }
        Paint(12, 10, 5, 5, 14); Check("DK2N022"); document.Undo();
        Paint(12, 10, 4, 5, 14, 4); Check("DK2N020");
        Expect(TerrainLayoutDiagnostics.Validate(document).Single().Severity == DiagnosticSeverity.Error, "Bad heart dimensions must be fatal."); document.Undo();
        Paint(12, 10, 5, 5, 14, 7); Check("DK2N025", "DK2N025", "DK2N025"); document.Undo();
        foreach (var owner in new byte[] { 1, 2 })
        {
            Paint(12, 10, 5, 5, 14, owner); Paint(20, 20, 5, 5, 14, owner); Check(); document.Undo(); document.Undo();
        }
        Paint(1, 1, 5, 5, 14, 1); Check("DK2N024"); document.Undo();
        // Original shape identity ignores ownership, including mixed-owner rows.
        Paint(2, 1, 1, 5, 14, 7); Check(); document.Undo();
        Paint(6, 1, 5, 5, 14, 4); Check("DK2N020"); document.Undo();
        // Unequal valid-width rows are fatal; expansion uses the minimum width.
        Paint(12, 10, 3, 3, 12); Paint(15, 11, 1, 1, 12);
        Check("DK2N019", "DK2N021");
        var irregular = TerrainLayoutDiagnostics.Validate(document).First();
        Expect(irregular.Severity == DiagnosticSeverity.Error && irregular.Region == new MapDiagnosticRegion(12, 10, 3, 3), "Non-rectangle region/severity differs from original expansion.");
        document.Undo(); document.Undo();
        // Row-width size failure precedes shape failure in the original compiler.
        Paint(12, 10, 3, 3, 12); Paint(14, 11, 1, 1, 2, 2); Check("DK2N021", "DK2N021", "DK2N021"); document.Undo(); document.Undo();
        // Native reserved bytes/project catalogue discriminators must never be
        // interpreted as terrain group IDs. Raw painting already retained 231.
        Check();
        var saved = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, legacy ? "project" : "native"));
        Check(); Expect(TerrainLayoutDiagnostics.Validate(NativeMapDocument.Load(saved)).Count == 0, "Save/reopen changed valid terrain checks.");

        // Independent slow raster reference with explicit original dimensions.
        // No bottom-up summaries or production catalogue/footprint helper used.
        List<(string Code, DiagnosticSeverity Severity, MapDiagnosticRegion? Region)> Reference()
        {
            var results = new List<(string, DiagnosticSeverity, MapDiagnosticRegion?)>(); var visited = new bool[32, 32]; var owners = new HashSet<byte>();
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++)
            {
                if (visited[x, y]) continue;
                var tile = document.GetTile(x, y); var matching = rules.Where(rule => rule.Terrain == tile.Terrain && rule.Variant == tile.Variant).ToArray();
                if (matching.Length == 0) continue;
                var rule = matching[0]; var rowWidths = new List<int>();
                for (var row = y; row < 32; row++)
                {
                    var columns = 0;
                    while (x + columns < 32 && document.GetTile(x + columns, row) is var next && next.Terrain == tile.Terrain && next.Variant == tile.Variant) columns++;
                    if (columns == 0) break; rowWidths.Add(columns);
                }
                var min = rowWidths.Min(); var region = new MapDiagnosticRegion(x, y, min, rowWidths.Count);
                if (tile.Terrain == 14 && !owners.Add(tile.Owner) && tile.Owner is not (1 or 2)) results.Add(("DK2N022", DiagnosticSeverity.Error, region));
                var failure = 0; var previous = -1;
                foreach (var rowWidth in rowWidths)
                {
                    if (rowWidth < rule.MinW || rule.MaxW != 0 && rowWidth > rule.MaxW) { failure = 1; break; }
                    if (previous != -1 && previous != rowWidth) { failure = 2; break; }
                    previous = rowWidth;
                }
                if (failure == 0 && (rowWidths.Count < rule.MinH || rule.MaxH != 0 && rowWidths.Count > rule.MaxH)) failure = 1;
                if (failure == 2) results.Add(("DK2N019", DiagnosticSeverity.Error, region));
                else if (failure == 1) results.Add((tile.Terrain == 14 ? "DK2N020" : "DK2N021", tile.Terrain == 14 ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning, region));
                for (var row = y; row < y + rowWidths.Count; row++) for (var column = x; column < x + min; column++) visited[column, row] = true;
            }
            if (owners.Count == 0) results.Add(("DK2N023", DiagnosticSeverity.Error, null));
            else
            {
                if (!owners.Contains(3)) results.Add(("DK2N024", DiagnosticSeverity.Error, null));
                for (byte owner = 4; owner <= 6; owner++) if (!owners.Contains(owner) && owners.Any(value => value > owner && value <= 7)) results.Add(("DK2N025", DiagnosticSeverity.Error, null));
            }
            return results;
        }
        var random = new Random(11419);
        for (var trial = 0; trial < 100; trial++)
        {
            document.Edit("Random malformed regions", () =>
            {
                document.Paint(MapDrawing.Rectangle(new(0, 0, 32, 32), true), new(2, 2, 1, 229));
                for (var shape = 0; shape < 35; shape++)
                {
                    var rule = rules[random.Next(rules.Length)]; var x = random.Next(32); var y = random.Next(32);
                    Paint(x, y, Math.Min(random.Next(1, 9), 32 - x), Math.Min(random.Next(1, 9), 32 - y), (byte)rule.Terrain, (byte)random.Next(1, 8), (byte)rule.Variant);
                }
            });
            var expected = Reference(); var actual = TerrainLayoutDiagnostics.Validate(document);
            Expect(actual.Select(item => (item.Code, item.Severity, item.Region)).SequenceEqual(expected), $"Terrain summaries disagree with original raster reference (legacy={legacy}, trial={trial}).");
            document.Undo(); Check();
        }
    }
    var large = NativeMapDocument.Create(1024, 1024, "LargeDiagnostics");
    large.Paint(MapDrawing.Rectangle(new(0, 0, 1024, 1024), true), new(29, 1, 1, 251));
    Expect(TerrainLayoutDiagnostics.Validate(large).Single().Code == "DK2N023", "An unlimited gate rectangle was treated as multiple regions or constrained.");
    Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(input), "Diagnostics changed the original fixture file.");
}

void TestTerrainPlacement()
{
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "placement.dkm");
    var input = TextMapFile.Parse("GUI_GENERATED_MAP\nPlacement\nPLAYERS 1\nMAP\n16 X 16\n" + string.Concat(Enumerable.Repeat(string.Concat(Enumerable.Repeat("Oa", 16)) + "\n", 16)) + "EOF").ToArray();
    File.WriteAllBytes(path, input);
    var rules = new[] { (14, 1, 5, 5, 5, 5), (12, 1, 3, 3, 5, 5), (40, 1, 3, 3, 5, 5), (28, 1, 3, 1, 3, 0),
        (33, 1, 2, 2, 2, 2), (34, 1, 3, 2, 3, 0), (37, 1, 3, 1, 3, 1), (37, 2, 1, 3, 1, 3) };
    foreach (var (terrain, variant, minW, minH, maxW, maxH) in rules)
    {
        var footprint = EditorTerrainRules.Footprint(new((byte)terrain, 7, (byte)variant));
        Expect(footprint == new EditorTerrainFootprint(minW, minH, maxW, maxH), "Terrain dimensions differ from original Datas_Map.c.");
        Expect(footprint.RequiresRectangle && footprint.FixedSize == (minW == maxW && minH == maxH), "Incorrect fixed-size/rectangle classification.");
        Expect(footprint.CursorRectangle(new(15, 15), 16, 16) == new TileRect(16 - minW, 16 - minH, minW, minH), "Minimum cursor did not shift at right/bottom edge.");
        var reversed = terrain switch { 12 or 40 => new TileRect(6, 6, 3, 3), 28 or 34 => new(6, 5, 3, 2), _ => new(6, 6, minW, minH) };
        Expect(footprint.GestureRectangle(new(6, 6), new(5, 5), 16, 16) == reversed, "Reversed gesture differs from the source: a two-cell axis already satisfies minimum two; fixed footprints stamp immediately.");
    }
    var portal = EditorTerrainRules.Footprint(new(12, 3, 1));
    Expect(portal.GestureRectangle(new(6, 6), new(2, 2), 16, 16) == new TileRect(2, 2, 5, 5)
        && portal.GestureRectangle(new(6, 6), new(14, 14), 16, 16) == new TileRect(6, 6, 5, 5), "Portal maximum did not retain drag direction.");
    Expect(EditorTerrainRules.Footprint(new(28, 1, 1)).GestureRectangle(new(6, 6), new(2, 2), 16, 16) == new TileRect(4, 2, 3, 5), "Gate with unlimited height was narrowed to its minimum.");
    Expect(!EditorTerrainRules.Footprint(new(29, 1, 1)).RequiresRectangle && !EditorTerrainRules.Footprint(new(99, 7, 2)).RequiresRectangle, "Unconstrained/unknown terrain received guessed footprint restrictions.");
    foreach (var legacy in new[] { false, true })
    {
        var document = legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(16, 16, "Placement");
        if (legacy) document.AddLegacyRecord("Creature", new(3, 3)); else document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEKEEPER"), new(3, 3));
        var root = document.EnsureScriptScope(document.ScriptOwners().Single(owner => owner.Scope == 2));
        document.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED"));
        document.Paint(MapDrawing.Rectangle(new(0, 0, 16, 16), true), new(2, 2, 1, 211));
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(IReadOnlyDictionary<NativeFile, byte[]> expected)
        { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(expected[file]), "Placement changed unrelated records/padding: " + file.ExportPath); }
        var changes = 0; EventHandler handler = (_, _) => changes++; document.Changed += handler;
        foreach (var (terrain, variant, minW, minH, maxW, maxH) in rules)
        foreach (var shape in Enum.GetValues<EditorPaintShape>())
        {
            var selected = new MapTile((byte)terrain, 7, (byte)variant, 219);
            var immediate = shape == EditorPaintShape.Brush || minW == maxW && minH == maxH;
            var width = immediate ? minW : maxW != 0 ? maxW : 8;
            var height = immediate ? minH : maxH != 0 ? maxH : 8;
            var expected = before.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            var owner = terrain is 28 or 33 or 34 or 37 ? (byte)1 : (byte)7;
            for (var y = 3; y < 3 + height; y++) for (var x = 3; x < 3 + width; x++) WriteUInt32(expected[document.TileFile], 36 + (y * 16 + x) * 4, new MapTile((byte)terrain, owner, (byte)variant, 219).Packed);
            var count = changes;
            Expect(document.PaintEditorGesture(new(3, 3), new(10, 10), selected, shape) == EditorPlacementResult.Painted, "A valid source footprint was rejected.");
            Exact(expected); Expect(changes == count + 1, "Footprint gesture was not one transaction.");
            Expect(document.PaintEditorGesture(new(3, 3), new(10, 10), selected, shape) == EditorPlacementResult.Painted && changes == count + 1, "Repeated identical gesture consumed history.");
            document.Undo(); Exact(before); document.Redo(); Exact(expected); document.Undo(); Exact(before);
            var undo = document.CanUndo; var redo = document.CanRedo; count = changes;
            foreach (var start in new[] { new TilePoint(0, 3), new TilePoint(15, 15) })
                Expect(document.PaintEditorGesture(start, new(10, 10), selected, shape) == EditorPlacementResult.ProtectedFootprint, "Bounded minimum footprint touching the border was accepted.");
            Expect(document.PaintEditorGesture(new(3, 3), new(10, 10), selected, shape, new(3, 3, 1, 1)) == EditorPlacementResult.SelectionClipsFootprint, "Selection produced a partial constrained footprint.");
            Exact(before); Expect(changes == count && document.CanUndo == undo && document.CanRedo == redo, "Rejected footprint changed bytes/notifications/redo.");
        }
        // The original tests the minimum cursor at press, not the expanded drag
        // at release: a taller gate may reach the edge, whose bytes stay protected.
        document.PaintEditorGesture(new(3, 3), new(3, 15), new(28, 7, 1), EditorPaintShape.Line);
        Expect(document.GetTile(3, 14) == new MapTile(28, 1, 1) && document.GetTile(3, 15) == new MapTile(2, 2, 1, 211), "Expanded drag border handling differs from original press/release ordering."); document.Undo(); Exact(before);
        Expect(document.PaintEditorGesture(new(3, 3), new(10, 10), new(12, 4, 1), EditorPaintShape.FilledRectangle, new(3, 3, 3, 3)) == EditorPlacementResult.SelectionClipsFootprint, "A larger final portal footprint was partially clipped."); Exact(before);
        foreach (var shape in Enum.GetValues<EditorPaintShape>())
        {
            var rectangle = new TileRect(2, 2, 5, 4);
            var points = shape switch { EditorPaintShape.Brush or EditorPaintShape.Line => MapDrawing.Line(new(2, 2), new(6, 5)),
                EditorPaintShape.Rectangle => MapDrawing.Rectangle(rectangle, false), EditorPaintShape.FilledRectangle => MapDrawing.Rectangle(rectangle, true),
                EditorPaintShape.Ellipse => MapDrawing.Ellipse(rectangle, false), _ => MapDrawing.Ellipse(rectangle, true) };
            if (RandomPaint.Supports(shape) && OperatingSystem.IsWindows()) points = OriginalGdiReference.Points(shape,new(2,2),new(6,5),rectangle);
            var expected = before.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            foreach (var point in points.Where(point => new TileRect(3, 3, 3, 2).Contains(point.X, point.Y))) WriteUInt32(expected[document.TileFile], 36 + (point.Y * 16 + point.X) * 4, new MapTile(4, 2, 1, 223).Packed);
            var priorChanges = changes; var previousRedo = document.CanRedo;
            document.PaintEditorGesture(new(2, 2), new(6, 5), new(4, 7, 2, 223), shape, new(3, 3, 3, 2)); Exact(expected);
            if (!expected[document.TileFile].AsSpan().SequenceEqual(before[document.TileFile])) document.Undo();
            else Expect(changes == priorChanges && document.CanRedo == previousRedo, "Selection-clipped empty shape consumed history/redo.");
            Exact(before);
        }
        IEnumerable<TilePoint> FailedStroke() { yield return new(3, 3); throw new InvalidOperationException("failed placement stroke"); }
        var prior = changes; var redoState = document.CanRedo;
        try { document.PaintEditorGesture(new(3, 3), new(5, 5), new(4, 7, 2), EditorPaintShape.Brush, brushPoints: FailedStroke()); throw new Exception("Failed stroke accepted."); }
        catch (InvalidOperationException exception) when (exception.Message == "failed placement stroke") { }
        Exact(before); Expect(changes == prior && document.CanRedo == redoState, "Failed ordinary gesture was partially committed.");
        document.PaintEditorGesture(new(3, 3), new(5, 5), new(14, 3, 1), EditorPaintShape.Brush, brushPoints: FailedStroke());
        Expect(document.GetTile(7, 7).Terrain == 14, "Fixed footprint was incorrectly interpreted as a freehand stroke.");
        // A second same-owner heart is still editable, just as in the original.
        document.PaintEditorGesture(new(9, 9), new(9, 9), new(14, 3, 1), EditorPaintShape.Brush);
        var saved = NativeMapDocument.Load(SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "placement-save-" + legacy)));
        Expect(saved.GetTile(7, 7).Terrain == 14 && saved.GetTile(13, 13).Terrain == 14 && saved.GetTile(0, 0).Reserved == 211, "Footprint save/reopen lost tiles or border padding.");
        if (legacy)
        {
            var compiled = document.CompileLegacyProject("Placement", NativeMapDocument.Create(16, 16, "PlacementBase"));
            Expect(compiled.GetTile(7, 7).Terrain == 14 && compiled.Records().Any(record => record.Category == "GAMEGATE" && record.Read(record.Fields.Single(field => field.Name == "bGateType")) == "14"), "Placed project heart did not reach native compiler output.");
        }
        document.Undo(); document.Undo(); Exact(before); document.Changed -= handler;
        if (legacy)
        {
            document.PaintEditorGesture(new(3, 3), new(3, 3), new(33, 7, 1), EditorPaintShape.Brush);
            document.PaintEditorGesture(new(9, 3), new(9, 3), new(37, 7, 1), EditorPaintShape.Brush);
            document.PaintEditorGesture(new(9, 9), new(9, 9), new(37, 7, 2), EditorPaintShape.Brush);
            var compiled = document.CompileLegacyProject("PlacedGates", NativeMapDocument.Create(16, 16, "GateBase"));
            Expect(!compiled.Records().Any(record => record.Category == "GAMEGATE") && compiled.GetTile(9, 9) == new MapTile(37, 1, 2),
                "Terrain placement invented a gate identity instead of retaining the original separate gate-record workflow.");
            document.Edit("Gate-region fixture", () =>
            {
                foreach (var point in new[] { new TilePoint(3, 3), new TilePoint(9, 3), new TilePoint(9, 9) }) document.AddLegacyRecord("Gate", point);
            });
            compiled = document.CompileLegacyProject("PlacedGates", NativeMapDocument.Create(16, 16, "GateBase"));
            string Value(NativeRecord record, string field) => record.Read(record.Fields.Single(candidate => candidate.Name == field));
            var gates = compiled.Records().Where(record => record.Category == "GAMEGATE").ToArray();
            Expect(gates.Count(record => Value(record, "bGateType") == "33") == 1 && gates.Count(record => Value(record, "bGateType") == "37") == 2
                && gates.Where(record => Value(record, "bGateType") == "37").Select(record => Value(record, "bGateOrientation")).Order().SequenceEqual(new[] { "0", "6" }),
                "Catalogue footprint/oriented variants did not reach generated native gate records.");
            document.Undo(); document.Undo(); document.Undo(); document.Undo(); Exact(before);
        }
    }
    var small = NativeMapDocument.Create(4, 4, "SmallPlacement");
    var smallBefore = small.Files.ToDictionary(file => file, file => file.Data.ToArray());
    Expect(small.PaintEditorGesture(new(1, 1), new(1, 1), new(14, 3, 1), EditorPaintShape.Brush) == EditorPlacementResult.MapTooSmall, "A too-small map received a partial heart.");
    foreach (var file in small.Files) Expect(file.Data.Span.SequenceEqual(smallBefore[file]), "Too-small footprint changed map bytes.");
    Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(input), "Footprint painting overwrote its original project.");
}

void TestSelectionProtection()
{
    using var fixture = new TemporaryFixture();
    var path = Path.Combine(fixture.DirectoryPath, "protected-selection.dkm");
    var input = TextMapFile.Parse("GUI_GENERATED_MAP\nMasks\nPLAYERS 1\nMAP\n6 X 6\n" + string.Concat(Enumerable.Repeat("OaOaOaOaOaOa\n", 6)) + "EOF").ToArray();
    File.WriteAllBytes(path, input);
    foreach (var legacy in new[] { false, true })
    {
        var document = legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(6, 6, "Masks");
        var category = legacy ? "Project.MAPTHING" : "GAMEKEEPER";
        foreach (var point in new[] { new TilePoint(0, 1), new TilePoint(1, 1), new TilePoint(2, 1), new TilePoint(4, 4), new TilePoint(5, 5) })
            if (legacy) document.AddLegacyRecord("Creature", point); else document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEKEEPER"), point);
        foreach (var owner in document.ScriptOwners().Where(owner => owner.Scope == 2).ToArray())
        { var root = document.EnsureScriptScope(owner); document.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED")); }
        for (var y = 0; y < 6; y++) for (var x = 0; x < 6; x++)
            document.Paint([new(x, y)], new((byte)(2 + y * 6 + x), (byte)(2 + x), (byte)(1 + y % 2), (byte)(170 + y * 6 + x)));
        // Terrain 30 in the interior is editable, not a transient protection flag.
        document.Paint([new(2, 2)], new(30, 7, 2, 219));
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(IReadOnlyDictionary<NativeFile, byte[]> expected)
        { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(expected[file]), "Protected selection changed unrelated bytes: " + file.ExportPath); }
        void Border()
        { for (var y = 0; y < 6; y++) for (var x = 0; x < 6; x++) if (document.IsEditorProtectedCell(x, y)) Expect(document.GetTile(x, y).Packed == BinaryPrimitives.ReadUInt32LittleEndian(before[document.TileFile].AsSpan(36 + (y * 6 + x) * 4)), "Selection rewrote protected border bytes."); }
        var rectangle = new TileRect(0, 0, 3, 3);
        var raw = document.CopySelection(rectangle);
        var clipboard = document.CopySelection(rectangle, protectBoundary: true);
        Expect(raw.ProtectedTileCount == 0 && raw.ObjectCount == 3 && clipboard.ProtectedTileCount == 5 && clipboard.ObjectCount == 2 && clipboard.ScriptCount == (legacy ? 4 : 2), "Source protection lost owner graphs or was inferred from native terrain/padding.");
        for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++) Expect(clipboard.IsProtectedTile(x, y) == (x == 0 || y == 0), "Incorrect selection mask position.");
        var bytes = SelectionFile.Serialize(clipboard); var parsed = SelectionFile.Parse(bytes);
        Expect(parsed.ProtectedTileCount == 5 && parsed.Tiles.Tiles.SequenceEqual(clipboard.Tiles.Tiles) && SelectionFile.Serialize(parsed).AsSpan().SequenceEqual(bytes), "DKCP v3 lost protected raw bytes or graph metadata.");
        var container = LegacySectionFile.Parse(bytes); var extension = container.Sections.Single(section => section.Signature == SelectionFile.ClipboardSection);
        Expect(extension.Version == 5, "Selection did not record the current historical-projection version.");
        var historical = container.ReadTiles();
        for (var i = 0; i < 9; i++) if(clipboard.IsProtectedTile(i % 3,i / 3))
            Expect(historical.Tiles[i] == new MapTile(30,2,1),"Legacy KMAP did not receive canonical protected terrain.");
        Expect(historical.Tiles.All(tile=>tile.Reserved==1),"Legacy KMAP leaked raw reserved bytes.");
        byte[] WithSections(IEnumerable<LegacySection> sections)
        { var result = LegacySectionFile.Create(sections).ToArray(); WriteUInt32(result, 0, LegacySectionFile.AreaFileSignature); return result; }
        byte[] WithExtension(int version, byte[] payload) => WithSections(container.Sections.Select(section => section.Signature == SelectionFile.ClipboardSection ? new LegacySection(section.Signature, version, payload) : section));
        var maskOffset = extension.Payload.Length - 9 * 4 - 5 * 4 - 9;
        foreach (var version in new[] { 1, 2 })
        {
            var length = version == 1 ? maskOffset - clipboard.ObjectCount - clipboard.ScriptCount : maskOffset;
            var old = SelectionFile.Parse(WithExtension(version, extension.Payload[..length].ToArray()));
            Expect(old.ProtectedTileCount == 0 && old.Tiles.Tiles.SequenceEqual(historical.Tiles), "Legacy DKCP guessed a mask from terrain 30 or source bounds.");
        }
        var originalDkz = SelectionFile.Parse(WithSections(container.Sections.Where(section => section.Signature != SelectionFile.ClipboardSection)));
        Expect(originalDkz.ProtectedTileCount == 0 && originalDkz.ObjectCount == 2, "Historical DKZ support guessed transient editor state.");
        void Invalid(byte[] data)
        { try { SelectionFile.Parse(data); throw new Exception("Malformed selection mask accepted."); } catch (InvalidDataException) { } }
        foreach (var flag in new byte[] { 2, 255 })
        { var payload = extension.Payload.ToArray(); payload[maskOffset] = flag; Invalid(WithExtension(3, payload)); }
        Invalid(WithExtension(3, extension.Payload[..(maskOffset + 8)].ToArray()));
        Invalid(WithExtension(3, extension.Payload[..^1].ToArray()));
        var extra = extension.Payload.ToArray().Append((byte)0).ToArray(); Invalid(WithExtension(3, extra));
        var kmap = container.Sections.Single(section => section.Signature == LegacySectionFile.TileSectionSignature);
        var changedMap = kmap.Payload.ToArray(); changedMap[8] ^= 1;
        Invalid(WithSections(container.Sections.Select(section => section.Signature == kmap.Signature ? new LegacySection(section.Signature, section.Version, changedMap) : section)));
        var cropped = parsed.Crop(new(1, 0, 2, 3));
        Expect(cropped.ProtectedTileCount == 2 && cropped.ObjectCount == 2 && cropped.ScriptCount == clipboard.ScriptCount && cropped.IsProtectedTile(0, 0) && !cropped.IsProtectedTile(0, 1), "Nonzero crop origin lost protection or graph ownership.");
        var tilesOnly = document.CopySelection(new(0, 0, 1, 1), protectBoundary: true);
        var converted = SelectionFile.ConvertToFormat(tilesOnly, !legacy);
        Expect(converted.ProtectedTileCount == 1 && SelectionFile.Parse(SelectionFile.Serialize(converted)).Tiles.Tiles.SequenceEqual(tilesOnly.Tiles.Tiles), "Script-free cross-format conversion discarded protected bytes.");
        var rectangular = document.CopySelection(new(0, 0, 3, 2), protectBoundary: true);
        foreach (var (rotate, horizontal, clockwise) in new[] { (true, false, true), (true, false, false), (false, true, true), (false, false, true) })
        {
            var result = rotate ? clockwise ? rectangular.RotateClockwise() : rectangular.RotateCounterclockwise() : rectangular.Mirror(horizontal);
            for (var y = 0; y < 2; y++) for (var x = 0; x < 3; x++)
            {
                var target = rotate ? clockwise ? new TilePoint(1 - y, x) : new(y, 2 - x) : horizontal ? new(2 - x, y) : new(x, 1 - y);
                Expect(result.IsProtectedTile(target.X, target.Y) == rectangular.IsProtectedTile(x, y)
                    && result.Tiles.Tiles[target.Y * result.Tiles.Width + target.X] == rectangular.Tiles.Tiles[y * 3 + x], "Nonsquare buffer transform misaligned mask/tiles.");
            }
            Expect(SelectionFile.Parse(SelectionFile.Serialize(result)).ProtectedTileCount == 4, "Nonsquare DKZ lost protection.");
        }
        var rotatedCrop = rectangular.RotateClockwise().Crop(new(0, 1, 2, 2));
        Expect(rotatedCrop.ProtectedTileCount == 2 && rotatedCrop.ObjectCount == 2 && SelectionFile.Parse(SelectionFile.Serialize(rotatedCrop)).ProtectedTileCount == 2, "Rotated nonzero crop lost the original mask/owners.");
        Expect(SelectionFile.Serialize(rectangular.RotateClockwise().RotateClockwise().RotateClockwise().RotateClockwise()).AsSpan().SequenceEqual(SelectionFile.Serialize(rectangular)), "Four nonsquare buffer turns did not restore protection/provenance.");
        var changes = 0; EventHandler handler = (_, _) => changes++; document.Changed += handler;
        document.PasteSelection(new(2, 2), parsed, respectEditorRules: true); Border();
        Expect(changes == 1 && document.GetTile(2, 2) == new MapTile(1, 2, 1) && document.GetTile(4, 4) == new MapTile(30, 7, 2, 219)
            && document.Records().Count(record => record.Category == category) == 6 && ScriptGraph.Build(document).Events.Count == 7,
            "Protected paste did not fill source masks, retain editable terrain 30 or clone only accepted graphs.");
        var pasted = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); document.Undo(); Exact(before); document.Redo(); Exact(pasted); document.Undo(); Exact(before);
        document.PasteSelection(new(4, 4), parsed, clipToMap: true, respectEditorRules: true); Border();
        Expect(document.GetTile(4, 4) == new MapTile(1, 2, 1) && document.Records().Count(record => record.Category == category) == 4
            && document.ScriptBindings().Count(binding => binding.Owner is { Scope: 2 }) == 4 && ScriptGraph.Build(document).Events.Count == 5,
            "Edge paste removed a protected destination owner, retained a rejected source graph or skipped masked-cell deletion.");
        var clipped = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); document.Undo(); Exact(before); document.Redo(); Exact(clipped); document.Undo(); Exact(before);
        var count = changes; var undo = document.CanUndo; var redo = document.CanRedo;
        document.PasteSelection(new(5, 5), parsed, clipToMap: true, respectEditorRules: true); Exact(before);
        Expect(changes == count && document.CanUndo == undo && document.CanRedo == redo, "Protected-target-only paste consumed history, IDs or redo.");
        foreach (var position in new[] { new TilePoint(-1, 1), new TilePoint(6, 1) })
        { try { document.PasteSelection(position, parsed, clipToMap: true, respectEditorRules: true); throw new Exception("Invalid protected paste origin accepted."); } catch (ArgumentOutOfRangeException) { } Exact(before); }
        document.PasteSelection(new(2, 2), raw); Expect(document.GetTile(2, 2) == raw.Tiles.Tiles[0], "Raw data paste was changed by editor policy."); document.Undo(); Exact(before);
        foreach (var (rotate, horizontal, clockwise) in new[] { (true, false, true), (true, false, false), (false, true, true), (false, false, true) })
        {
            TilePoint Map(int x, int y) => rotate ? clockwise ? new(2 - y, x) : new(y, 2 - x) : horizontal ? new(2 - x, y) : new(x, 2 - y);
            var transformed = rotate ? clockwise ? parsed.RotateClockwise() : parsed.RotateCounterclockwise() : parsed.Mirror(horizontal);
            for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++)
            { var target = Map(x, y); Expect(transformed.IsProtectedTile(target.X, target.Y) == (x == 0 || y == 0), "Rotation/reflection misaligned protection mask."); }
            Expect(SelectionFile.Serialize(SelectionFile.Parse(SelectionFile.Serialize(transformed))).AsSpan().SequenceEqual(SelectionFile.Serialize(transformed)), "Transformed mask DKZ did not reopen exactly.");
            var expected = before.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++)
            {
                var target = Map(x, y); if (document.IsEditorProtectedCell(target.X, target.Y)) continue;
                var tile = x == 0 || y == 0 ? new MapTile(1, 2, 1) : raw.Tiles.Tiles[y * 3 + x];
                WriteUInt32(expected[document.TileFile], 36 + (target.Y * 6 + target.X) * 4, tile.Packed);
            }
            // Original transforms move ALL selected physical things before applying
            // tile protection, including things moving to/from the outer edge.
            foreach (var record in document.Records().Where(record => record.Category == category))
            {
                var xField = record.Fields.Single(field => field.Name == "x" || field.Name.EndsWith("LocationX", StringComparison.Ordinal));
                var yField = record.Fields.Single(field => field.Name == "y" || field.Name.EndsWith("LocationY", StringComparison.Ordinal));
                var x = int.Parse(record.Read(xField)); var y = int.Parse(record.Read(yField)); if (!rectangle.Contains(x, y)) continue;
                var point = Map(x, y); xField.Encode(point.X.ToString()).CopyTo(expected[record.File], record.Offset + xField.Offset); yField.Encode(point.Y.ToString()).CopyTo(expected[record.File], record.Offset + yField.Offset);
            }
            count = changes; document.TransformSelection(rectangle, rotate, horizontal, clockwise, respectEditorRules: true); Exact(expected); Border();
            Expect(changes == count + 1, "Protected transform did not commit exactly once.");
            document.Undo(); Exact(before); document.Redo(); Exact(expected); document.Undo(); Exact(before);
        }
        Expect(SelectionFile.Serialize(clipboard).AsSpan().SequenceEqual(bytes), "Protection operations mutated the independent buffer.");
        document.PasteSelection(new(2, 2), parsed, respectEditorRules: true);
        var saved = NativeMapDocument.Load(SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "mask-save-" + legacy)));
        for (var y = 0; y < 6; y++) for (var x = 0; x < 6; x++) Expect(saved.GetTile(x, y) == document.GetTile(x, y), "Masked selection save/reopen changed tile bytes.");
        if (legacy)
        {
            var compiled = document.CompileLegacyProject("Masks", NativeMapDocument.Create(6, 6, "MaskBase"));
            for (var y = 0; y < 6; y++) for (var x = 0; x < 6; x++) Expect(compiled.GetTile(x, y) == document.GetTile(x, y), "Mask policy was confused with compiler/native tile padding.");
        }
        document.Undo(); Exact(before); document.Changed -= handler;
    }
    Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(input), "Protected-selection editing overwrote the original project.");
}

void TestEditorTerrainRules()
{
    using var fixture = new TemporaryFixture();
    var basis = TextMapFile.Parse("GUI_GENERATED_MAP\nEditorTerrain\nPLAYERS 1\nMAP\n6 X 6\n" + string.Concat(Enumerable.Repeat("OaOaOaOaOaOa\n", 6)) + "EOF");
    var path = Path.Combine(fixture.DirectoryPath, "editor-terrain.dkm"); File.WriteAllBytes(path, basis.ToArray()); var input = File.ReadAllBytes(path);
    foreach (var item in LegacyCompilerRules.Elements("NewMap"))
        Expect(EditorTerrainRules.DecodeFillPreset(item.ID) == new MapTile((byte)(item.ID >> 24), (byte)(item.ID >> 16), (byte)(item.ID >> 8)), "Map background preset differs from source catalogue.");
    try { EditorTerrainRules.DecodeFillPreset(0xFE020101); throw new Exception("An unknown editor fill preset was guessed."); } catch (InvalidDataException) { }
    foreach (var item in LegacyCompilerRules.Elements("World").Concat(LegacyCompilerRules.Elements("Gates")).Where(item => (byte)(item.ID >> 24) != 0xFE))
    {
        var selected = new MapTile((byte)(item.ID >> 24), 7, (byte)(item.ID >> 8), 213);
        Expect(EditorTerrainRules.NormalizePaint(selected) == selected with { Owner = (byte)(item.ID >> 16) }, "Fixed-owner world/gate catalogue was not respected.");
    }
    Expect(EditorTerrainRules.NormalizePaint(new(8, 6, 1, 211)) == new MapTile(8, 6, 1, 211) && EditorTerrainRules.NormalizePaint(new(11, 5, 1, 212)) == new MapTile(11, 5, 1, 212), "Owned paths or rooms were forced neutral.");
    foreach (var legacy in new[] { false, true })
    {
        var document = legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(6, 6, "EditorTerrain");
        NativeRecord Add(TilePoint point) => legacy ? document.AddLegacyRecord("Creature", point) : document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEKEEPER"), point);
        Add(new(0, 1)); Add(new(1, 1));
        foreach (var owner in document.ScriptOwners().Where(owner => owner.Scope == 2).ToArray())
        {
            var root = document.EnsureScriptScope(owner); document.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED"));
        }
        var info = legacy ? document.Records().Single(record => record.Category == "Project.MAPPROPERTIES") : null;
        if (info is not null) document.SetField(info, info.Fields.Single(field => field.Name == "FillID"), 0x06020101u.ToString());
        // Raw imported border/padding must survive editor operations exactly.
        document.Paint(MapDrawing.Rectangle(new(0, 0, 6, 6), true), new(30, 4, 2, 217));
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        void Exact(IReadOnlyDictionary<NativeFile, byte[]> expected)
        { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(expected[file]), "Editor terrain operation changed unrelated bytes: " + file.ExportPath); }
        void Border()
        {
            for (var y = 0; y < 6; y++) for (var x = 0; x < 6; x++)
                if (document.IsEditorProtectedCell(x, y)) Expect(document.GetTile(x, y) == new MapTile(30, 4, 2, 217), "Protected map border was normalized or painted.");
        }
        var changes = 0; EventHandler handler = (_, _) => changes++; document.Changed += handler;
        document.PaintEditor(MapDrawing.Rectangle(new(0, 0, 6, 6), true), new(4, 7, 2, 209)); Border();
        Expect(changes == 1 && document.GetTile(1, 1) == new MapTile(4, 2, 1, 209), "Editor paint did not normalize water or commit once.");
        var painted = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); document.Undo(); Exact(before); document.Redo(); Exact(painted); document.Undo(); Exact(before);
        var undo = document.CanUndo; var redo = document.CanRedo; var count = changes;
        document.PaintEditor([new(0, 0), new(5, 5), new(-1, 1), new(6, 1)], new(6, 7, 2));
        document.FloodFillEditor(new(0, 1), new(5, 7, 2)); Exact(before);
        Expect(changes == count && document.CanUndo == undo && document.CanRedo == redo, "Border-only painting/fill changed history or notifications.");
        IEnumerable<TilePoint> FailingStroke() { yield return new(1, 1); throw new InvalidOperationException("synthetic stroke failure"); }
        try { document.PaintEditor(FailingStroke(), new(6, 7, 2)); throw new Exception("A failed stroke was swallowed."); } catch (InvalidOperationException exception) when (exception.Message == "synthetic stroke failure") { }
        Exact(before); Expect(changes == count && document.CanRedo == redo, "A failed editor stroke leaked partial tile changes or consumed redo.");
        document.FloodFillEditor(new(1, 1), new(5, 7, 2, 203), new(1, 1, 2, 2)); Border();
        for (var y = 1; y < 5; y++) for (var x = 1; x < 5; x++)
            Expect(document.GetTile(x, y) == (x < 3 && y < 3 ? new MapTile(5, 2, 1, 203) : new MapTile(30, 4, 2, 217)), "Editor flood fill ignored its selection or treated the reserved byte as a catalogue selector.");
        document.Undo(); Exact(before);
        var selection = document.CopySelection(new(0, 0, 6, 6), protectBoundary: true);
        Expect(selection.ObjectCount == 1 && selection.ScriptCount == (legacy ? 2 : 1), "Protected copy included a border object/graph or lost its interior owner.");
        Expect(document.CopySelection(new(0, 0, 6, 6)).ObjectCount == 2, "Raw lossless selection access was silently changed.");
        foreach (var special in new byte[] { 14, 12, 40, 28, 33, 34, 37 })
        {
            document.CutSelection(new(0, 0, 6, 6), new(special, 7, 2), respectEditorRules: true); Border();
            Expect(document.GetTile(1, 1) == new MapTile(legacy ? (byte)6 : (byte)2, 2, 1), "Cut created heart/portal tiles instead of the map's background preset.");
            var remaining = document.Records().Single(record => record.Category == (legacy ? "Project.MAPTHING" : "GAMEKEEPER"));
            Expect(remaining.Read(remaining.Fields.Single(field => field.Name == (legacy ? "x" : "dwKeeperLocationX"))) == "0"
                && document.ScriptBindings().Count(binding => binding.Owner is { Scope: 2 }) == 1, "Cut removed a border object/scope or retained its interior owner.");
            var cut = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); document.Undo(); Exact(before); document.Redo(); Exact(cut); document.Undo(); Exact(before);
        }
        document.CutSelection(new(0, 0, 6, 6), new(4, 7, 2), respectEditorRules: true);
        Expect(document.GetTile(1, 1) == new MapTile(4, 2, 1), "Ordinary cut fill did not use the normalized selected terrain."); document.Undo(); Exact(before);
        try { document.CutSelection(new(5, 5, 2, 2), new(4, 7, 2), respectEditorRules: true); throw new Exception("An out-of-bounds cut was accepted."); } catch (ArgumentOutOfRangeException) { }
        Exact(before);
        if (legacy)
        {
            foreach (var preset in LegacyCompilerRules.Elements("NewMap"))
            {
                var fillField = info!.Fields.Single(field => field.Name == "FillID"); var presetValue = unchecked((int)preset.ID).ToString(); var previousPreset = info.Read(fillField);
                document.SetField(info, fillField, presetValue);
                document.CutSelection(new(0, 0, 6, 6), new(14, 7, 2), respectEditorRules: true);
                Expect(document.GetTile(1, 1) == EditorTerrainRules.DecodeFillPreset(preset.ID), "Special cut lost the full background terrain/owner/variant preset."); Border();
                document.Undo(); if (presetValue != previousPreset) document.Undo(); Exact(before);
            }
            document.SetField(info!, info!.Fields.Single(field => field.Name == "FillID"), "0");
            var invalid = document.Files.ToDictionary(file => file, file => file.Data.ToArray()); var notifications = changes;
            try { document.CutSelection(new(0, 0, 6, 6), new(14, 7, 2), respectEditorRules: true); throw new Exception("Unknown special-cut background was guessed."); } catch (InvalidDataException) { }
            Exact(invalid); Expect(changes == notifications, "Invalid cut background removed objects or emitted a notification."); document.Undo(); Exact(before);
        }
        document.PaintEditor([new(1, 1)], new(4, 7, 2, 219));
        var exportedState = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var loaded = NativeMapDocument.Load(SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "editor-terrain-" + legacy)));
        Expect(loaded.GetTile(0, 1) == new MapTile(30, 4, 2, 217) && loaded.GetTile(1, 1) == new MapTile(4, 2, 1, 219), "Editor paint export/reopen altered border or reserved bytes.");
        if (legacy)
        {
            var compiled = document.CompileLegacyProject("EditorTerrain", NativeMapDocument.Create(6, 6, "TerrainBase"));
            Expect(compiled.GetTile(0, 1) == document.GetTile(0, 1) && compiled.GetTile(1, 1) == document.GetTile(1, 1), "Editor paint compilation did not preserve actual tile bytes.");
        }
        Exact(exportedState); document.Undo(); Exact(before);
        document.Changed -= handler;
    }
    Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(input), "Editor painting/cut overwrote the original project.");
}

void TestSelectionTransforms()
{
    using var fixture = new TemporaryFixture();
    var basis = TextMapFile.Parse("GUI_GENERATED_MAP\nTransforms\nPLAYERS 1\nMAP\n8 X 8\n" + string.Concat(Enumerable.Repeat("OaOaOaOaOaOaOaOa\n", 8)) + "EOF");
    var path = Path.Combine(fixture.DirectoryPath, "transforms.dkm"); File.WriteAllBytes(path, basis.ToArray()); var originalFile = File.ReadAllBytes(path);
    foreach (var legacy in new[] { false, true })
    {
        var document = legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(8, 8, "Transforms");
        var things = new List<NativeRecord>();
        foreach (var (kind, native, point) in new[]
        {
            ("Creature", "GAMEKEEPER", new TilePoint(2, 2)), ("Door", "GAMEDOOR", new TilePoint(3, 2)),
            ("Trap", "GAMETRAP", new TilePoint(4, 2)), ("Object", "GAMEOBJECT", new TilePoint(5, 2)),
            ("MagicalObject", "GAMEOBJECT", new TilePoint(2, 3)), ("Creature", "GAMENEUTRAL", new TilePoint(3, 3)),
            ("Creature", "GAMEHERO", new TilePoint(4, 3)), ("Creature", "GAMEKEEPER", new TilePoint(0, 1))
        })
            things.Add(legacy ? document.AddLegacyRecord(kind, point) : document.AddRecord(RecordTemplate.All.Single(item => item.Structure == native), point));
        // Other rectangle-based categories are not moved by original selection transforms.
        foreach (var (kind, native) in new[] { ("Area", "GAMEAREA"), ("Band", "GAMEBAND"), ("Gate", "GAMEGATE") })
            if (legacy) document.AddLegacyRecord(kind, new(2, 2)); else document.AddRecord(RecordTemplate.All.Single(item => item.Structure == native), new(2, 2));
        var owner = document.ScriptOwners().First(item => item.Scope == 2);
        var root = document.EnsureScriptScope(owner);
        var conditionID = ScriptGraph.ID(document.InsertScriptEvent(root.Owner, root.Field, LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED")));
        var coordinates = new List<(NativeRecord Record, NativeField X, NativeField Y)>();
        foreach (var (name, point) in new[] { ("ACTION_ADDCREATURE", new TilePoint(2, 3)), ("ACTION_CHANGESQUARE", new TilePoint(3, 4)),
            ("ACTION_CHANGEOWNER", new TilePoint(4, 5)), ("ACTION_ADDCREATURE", new TilePoint(0, 1)) })
        {
            var child = ScriptGraph.Build(document).Events[conditionID].Edges.Single(edge => edge.Field.Name is "and" or "ChildAnd");
            var action = document.InsertScriptEvent(child.Owner, child.Field, LegacyEventSchemas.All.Single(type => type.Name == name));
            document.SetField(action, action.Fields.Single(field => field.Name == (legacy ? "param5" : "Param5")), point.X.ToString());
            document.SetField(action, action.Fields.Single(field => field.Name == (legacy ? "param7" : "Param7")), point.Y.ToString());
            if (!legacy) document.SetField(action, action.Fields.Single(field => field.Name == "ChildAnd"), "60000");
        }
        // Collect fresh offsets after all scenario insertions.
        foreach (var thing in things)
        {
            var record = document.Records().Single(item => item.File == thing.File && item.Offset == thing.Offset && item.Category == thing.Category);
            coordinates.Add((record, record.Fields.Single(field => field.Name == "x" || field.Name.EndsWith("LocationX", StringComparison.Ordinal)),
                record.Fields.Single(field => field.Name == "y" || field.Name.EndsWith("LocationY", StringComparison.Ordinal))));
        }
        foreach (var record in ScriptGraph.Build(document).Events.Values.Where(item => item.Type?.RecordSignature == 0xD6).Select(item => item.Record))
            coordinates.Add((record, record.Fields.Single(field => field.Name == (legacy ? "param5" : "Param5")), record.Fields.Single(field => field.Name == (legacy ? "param7" : "Param7"))));
        var rectangle = new TileRect(2, 2, 4, 4);
        for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++) document.Paint([new(2 + x, 2 + y)], new((byte)(1 + y * 4 + x), (byte)(2 + x), (byte)(1 + y % 2), (byte)(100 + y * 4 + x)));
        var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var clipboard = document.CopySelection(rectangle); var bufferBytes = SelectionFile.Serialize(clipboard);
        void Exact(IReadOnlyDictionary<NativeFile, byte[]> expected)
        { foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(expected[file]), "Selection transform changed unrelated bytes: " + file.ExportPath); }
        foreach (var (rotate, horizontal, clockwise) in new[] { (true, false, true), (true, false, false), (false, true, true), (false, false, true) })
        {
            var expected = before.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            TilePoint Map(int x, int y) => rotate ? clockwise ? new(7 - y, x) : new(y, 7 - x) : horizontal ? new(7 - x, y) : new(x, 7 - y);
            for (var y = 2; y < 6; y++) for (var x = 2; x < 6; x++)
            {
                var point = Map(x, y); var offset = 36 + (point.Y * 8 + point.X) * 4;
                before[document.TileFile].AsSpan(36 + (y * 8 + x) * 4, 4).CopyTo(expected[document.TileFile].AsSpan(offset, 4));
            }
            foreach (var (record, xField, yField) in coordinates)
            {
                var x = int.Parse(xField.Read(before[record.File].AsSpan(record.Offset, record.Length)));
                var y = int.Parse(yField.Read(before[record.File].AsSpan(record.Offset, record.Length)));
                if (!rectangle.Contains(x, y)) continue;
                var point = Map(x, y); xField.Encode(point.X.ToString()).CopyTo(expected[record.File], record.Offset + xField.Offset); yField.Encode(point.Y.ToString()).CopyTo(expected[record.File], record.Offset + yField.Offset);
            }
            var changes = 0; EventHandler handler = (_, _) => changes++; document.Changed += handler;
            document.TransformSelection(rectangle, rotate, horizontal, clockwise); Exact(expected);
            Expect(changes == 1, "A transform did not commit as one map notification.");
            document.Undo(); Exact(before); document.Redo(); Exact(expected); document.Undo(); Exact(before); document.Changed -= handler;
            // Exact bytes also prove identities, scope roots, backlinks and reserved payload did not change.
            Expect(SelectionFile.Serialize(clipboard).AsSpan().SequenceEqual(bufferBytes), "In-place transform mutated a previously captured buffer.");
        }
        document.TransformSelection(rectangle, true); document.TransformSelection(rectangle, true, clockwise: false); Exact(before);
        document.TransformSelection(rectangle, true, clockwise: false); document.TransformSelection(rectangle, true, clockwise: false);
        document.TransformSelection(rectangle, true, clockwise: false); document.TransformSelection(rectangle, true, clockwise: false); Exact(before);
        var undo = document.CanUndo; var redo = document.CanRedo; var rejectedChanges = 0; document.Changed += (_, _) => rejectedChanges++;
        foreach (var (rect, rotate, horizontal) in new[] { (new TileRect(2, 2, 4, 3), true, false), (new TileRect(2, 2, 1, 4), false, true),
            (new TileRect(2, 2, 4, 1), false, false), (new TileRect(7, 7, 2, 2), true, false) })
        {
            try { document.TransformSelection(rect, rotate, horizontal); throw new Exception("An invalid original transform shape was accepted."); } catch (ArgumentException) { }
            Exact(before);
        }
        Expect(rejectedChanges == 0 && document.CanUndo == undo && document.CanRedo == redo, "Rejected transforms changed history or emitted map notifications.");
        // Explicit clipboard rotations support arbitrary rectangles independently of in-place commands.
        var narrow = document.CopySelection(new(2, 2, 4, 3)); var narrowBytes = SelectionFile.Serialize(narrow);
        var restored = narrow.RotateClockwise().RotateCounterclockwise();
        Expect(SelectionFile.Serialize(restored).AsSpan().SequenceEqual(narrowBytes), "Opposite non-square clipboard transforms did not restore exact object/script bytes.");
        var counter = narrow.RotateCounterclockwise(); var target = legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(8, 8, "TransformedPaste");
        target.PasteSelection(new(1, 1), SelectionFile.Parse(SelectionFile.Serialize(counter)));
        Expect(target.GetTile(1, 1) == document.GetTile(5, 2), "Counterclockwise DKZ paste did not align its first tile.");
        NativeRecord FindAction(NativeMapDocument map, string name) => ScriptGraph.Build(map).Events.Values.Single(item => item.Type?.Name == name).Record;
        string Coordinate(NativeRecord record, bool x) => record.Read(record.Fields.Single(field => field.Name == (legacy ? x ? "param5" : "param7" : x ? "Param5" : "Param7")));
        var external = FindAction(target, "ACTION_CHANGEOWNER");
        Expect(Coordinate(external, true) == "4" && Coordinate(external, false) == "5", "Rotated DKZ rebased an external target that happens to be inside the new bounds.");
        var croppedRotation = SelectionFile.Parse(SelectionFile.Serialize(counter)).Crop(new(0, 3, 3, 1));
        var cropTarget = legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(8, 8, "RotatedCrop");
        cropTarget.PasteSelection(new(1, 1), SelectionFile.Parse(SelectionFile.Serialize(croppedRotation)));
        external = FindAction(cropTarget, "ACTION_CHANGEOWNER");
        var outsideCrop = FindAction(cropTarget, "ACTION_CHANGESQUARE");
        Expect(Coordinate(external, true) == "4" && Coordinate(external, false) == "5" && Coordinate(outsideCrop, true) == "4" && Coordinate(outsideCrop, false) == "4",
            "Cropping a rotated snapshot localized an external target or moved a formerly local point outside the crop.");
        var persisted = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "transforms-" + legacy));
        var loaded = NativeMapDocument.Load(persisted);
        // The raw legacy container/KMAP section is regenerated from the editable
        // tile buffer; compare that buffer and every non-derived section instead.
        if(legacy)SaveProjectionChecks.RoundTrip(document,loaded);
        else foreach(var file in loaded.Files)Expect(file.Data.Span.SequenceEqual(document.Files.Single(item=>item.ExportPath==file.ExportPath).Data.Span),"Native transform save/reopen changed exact records: "+file.ExportPath);
        if (legacy) Expect(ScriptGraph.Build(document.CompileLegacyProject("Transforms", NativeMapDocument.Create(8, 8, "Base"))).Events.Count == 5, "Transformed project graphs failed to compile.");
    }
    Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(originalFile), "Transforms overwrote the source project.");
}

void TestSelectionClipping()
{
    using var fixture = new TemporaryFixture();
    var basis = TextMapFile.Parse("GUI_GENERATED_MAP\nClipping\nPLAYERS 1\nMAP\n6 X 6\nOaOaOaOaOaOa\nOaOaOaOaOaOa\nOaOaOaOaOaOa\nOaOaOaOaOaOa\nOaOaOaOaOaOa\nOaOaOaOaOaOa\nEOF");
    var path = Path.Combine(fixture.DirectoryPath, "clipping.dkm"); File.WriteAllBytes(path, basis.ToArray());
    var timer = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED");
    foreach (var legacy in new[] { false, true })
    {
        NativeMapDocument New() => legacy ? NativeMapDocument.Load(path) : NativeMapDocument.Create(6, 6, "Clipping");
        var source = New();
        foreach (var (kind, native, point) in new[] { ("Creature", "GAMEKEEPER", new TilePoint(1, 1)), ("Creature", "GAMEKEEPER", new TilePoint(4, 4)),
            ("Door", "GAMEDOOR", new TilePoint(2, 1)), ("Object", "GAMEOBJECT", new TilePoint(4, 1)), ("MagicalObject", "GAMEOBJECT", new TilePoint(1, 2)), ("Trap", "GAMETRAP", new TilePoint(3, 3)) })
        {
            var thing = legacy ? source.AddLegacyRecord(kind, point) : source.AddRecord(RecordTemplate.All.Single(item => item.Structure == native), point);
            if (kind == "MagicalObject") source.SetField(thing, thing.Fields.Single(field => field.Name == (legacy ? "id" : "bObjectID")), ValueCatalogues.ForTemplate("MagicalObject").First().Value);
        }
        foreach (var owner in source.ScriptOwners().Where(owner => owner.Scope is 2 or 64 or 128 or 256).ToArray())
        { var root = source.EnsureScriptScope(owner); source.InsertScriptEvent(root.Owner, root.Field, timer); }
        if (legacy)
        {
            var map = source.EnsureScriptScope(source.ScriptOwners().Single(owner => owner.Scope == 32)); source.InsertScriptEvent(map.Owner, map.Field, timer);
            var creature = source.Records().First(item => item.Category == "Project.MAPTHING" && item.Read(item.Fields.Single(field => field.Name == "node.type")) == "0");
            var uid = creature.Read(creature.Fields.Single(field => field.Name == "uniqueid"));
            // Stale unused fields in a map binding are not ownership of a creature.
            source.SetField(map.Owner, map.Owner.Fields.Single(field => field.Name == "creature"), uid);
        }
        source.Paint(MapDrawing.Rectangle(new(1, 1, 4, 4), true), new(6, 3, 1));
        var before = source.Files.ToDictionary(file => file, file => file.Data.ToArray());
        var clipboard = source.CopySelection(new(1, 1, 4, 4)); var originalBytes = SelectionFile.Serialize(clipboard);
        Expect(clipboard.ObjectCount == 6 && clipboard.ScriptCount == (legacy ? 10 : 5), "Copy inferred a scenario owner from an unused field or lost actual scopes.");
        var cropped = clipboard.Crop(new(0, 0, 2, 2));
        Expect(cropped.ObjectCount == 3 && cropped.ScriptCount == (legacy ? 6 : 3) && cropped.Tiles.Width == 2 && cropped.Tiles.Height == 2, "Cropping retained clipped-out objects/graphs or dropped retained graphs.");
        var roundTrip = SelectionFile.Parse(SelectionFile.Serialize(cropped));
        Expect(roundTrip.ObjectCount == 3 && roundTrip.ScriptCount == cropped.ScriptCount, "Cropped selection did not survive DKZ round-trip.");
        var middle = clipboard.Crop(new(1, 0, 2, 2));
        var middleMap = New(); middleMap.PasteSelection(new(0, 0), middle);
        var door = middleMap.Records().Single(record => record.Category == (legacy ? "Project.MAPTHING" : "GAMEDOOR"));
        Expect(door.Read(door.Fields.Single(field => field.Name == (legacy ? "x" : "dwDoorLocationX"))) == "0", "Nonzero local cropping moved the retained object relative to the wrong origin.");
        var destination = New();
        if (legacy)
        {
            var orphan = destination.AddLegacyRecord("TriggerLink", new(0, 0));
            destination.SetField(orphan, orphan.Fields.Single(field => field.Name == "node.type"), "2");
            destination.SetField(orphan, orphan.Fields.Single(field => field.Name == "creature"), "1");
        }
        var destinationBefore = destination.Files.ToDictionary(file => file, file => file.Data.ToArray());
        destination.PasteSelection(new(5, 5), clipboard, clipToMap: true);
        Expect(destination.GetTile(5, 5).Terrain == 6 && destination.Records().Count(record => record.Category == (legacy ? "Project.MAPTHING" : "GAMEKEEPER")) == 1,
            "Clipped paste did not retain exactly the fitting tile/object.");
        Expect(ScriptGraph.Build(destination).Events.Count == 1, "Clipped-out objects left unrelated scenario records in the destination.");
        if (legacy)
        {
            Expect(destination.ScriptBindings().Single(binding => binding.Root.Target != 0).Owner is { Scope: 2 }, "Clipped project paste attached a wrong owner scope.");
            Expect(destination.ScriptBindings().Single(binding => binding.Root.Target == 0).Owner is null, "Paste acquired an orphan owner binding through reused unique identity.");
        }
        var after = destination.Files.ToDictionary(file => file, file => file.Data.ToArray()); destination.Undo();
        foreach (var file in destination.Files) Expect(file.Data.Span.SequenceEqual(destinationBefore[file]), "Clipped paste undo was not one exact transaction.");
        destination.Redo(); foreach (var file in destination.Files) Expect(file.Data.Span.SequenceEqual(after[file]), "Clipped paste redo changed bytes.");
        destination.Undo();
        try { destination.PasteSelection(new(-1, 0), clipboard, clipToMap: true); throw new Exception("A negative paste origin was accepted."); } catch (ArgumentOutOfRangeException) { }
        try { clipboard.Crop(new(0, 0, 5, 1)); throw new Exception("Out-of-range clipboard cropping was accepted."); } catch (ArgumentOutOfRangeException) { }
        foreach (var file in destination.Files) Expect(file.Data.Span.SequenceEqual(destinationBefore[file]), "Rejected clipping changed map buffers.");
        foreach (var file in source.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Clipping or cross-map paste altered the original source.");
        Expect(SelectionFile.Serialize(clipboard).AsSpan().SequenceEqual(originalBytes), "Clipping/paste mutated the retained copy buffer.");
        destination.Redo();
        var saved = SaveProjectionChecks.SaveCopy(destination, Path.Combine(fixture.DirectoryPath, "clipped-" + legacy));
        Expect(ScriptGraph.Build(NativeMapDocument.Load(saved)).Events.Count == 1, "The clipped pasted graph did not survive save/reopen.");
        if (legacy) Expect(ScriptGraph.Build(destination.CompileLegacyProject("Clipped", NativeMapDocument.Create(6, 6, "Basis"))).Events.Count == 1, "Clipped project scopes did not compile into the native graph.");
        if (legacy)
        {
            var container = LegacySectionFile.Parse(originalBytes); var payload = container.Sections.Single(section => section.Signature == SelectionFile.ClipboardSection).Payload.ToArray();
            var offset = 28; var changed = false;
            for (var recordIndex = 0; recordIndex < clipboard.ObjectCount + clipboard.ScriptCount; recordIndex++)
            {
                var nameLength = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(offset + 4)); var dataLength = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(offset + 8));
                var category = Encoding.UTF8.GetString(payload, offset + 12, nameLength); var dataOffset = offset + 12 + nameLength;
                if (category.StartsWith("Project.TRIGGERLINK", StringComparison.Ordinal))
                { WriteUInt32(payload, dataOffset, 0); var field = LegacyProjectSchemas.Fields(category[8..]).Single(field => field.Name == "node.type"); field.Encode("32").CopyTo(payload, dataOffset + field.Offset); changed = true; break; }
                offset = dataOffset + dataLength;
            }
            Expect(changed, "Malformed scope fixture has no link.");
            var malformed = LegacySectionFile.Create(container.Sections.Select(section => section.Signature == SelectionFile.ClipboardSection ? new LegacySection(section.Signature, section.Version, payload) : section)).ToArray();
            WriteUInt32(malformed, 0, LegacySectionFile.AreaFileSignature);
            WriteUInt32(malformed, 0, LegacySectionFile.AreaFileSignature);
            try { SelectionFile.Parse(malformed); throw new Exception("A DKZ injected a map-scope link without a selected owner."); } catch (InvalidDataException) { }
        }
    }
    // V2 link stride must not be inserted into a V3 event collection (or vice
    // versa). Both source buffers and any partially inserted destination things
    // must survive rejection unchanged.
    var v2Path = Path.Combine(fixture.DirectoryPath, "v2-clipping.dkm");
    File.WriteAllBytes(v2Path, LegacySectionFile.Create(basis.Sections.Where(section => section.Signature != NativeMapDocument.LegacyEvents).Append(new(NativeMapDocument.LegacyEvents, 2, new byte[12 + 146 * 64]))).ToArray());
    foreach (var (from, to) in new[] { (v2Path, path), (path, v2Path) })
    {
        var source = NativeMapDocument.Load(from); source.AddLegacyRecord("Creature", new(1, 1));
        var owner = source.ScriptOwners().Single(item => item.Scope == 2); var root = source.EnsureScriptScope(owner); source.InsertScriptEvent(root.Owner, root.Field, timer);
        var selection = source.CopySelection(new(0, 0, 3, 3)); var bytes = SelectionFile.Serialize(selection);
        var destination = NativeMapDocument.Load(to); var before = destination.Files.ToDictionary(file => file, file => file.Data.ToArray());
        try { destination.PasteSelection(new(1, 1), selection); throw new Exception("A mismatched project link stride was accepted."); } catch (InvalidDataException) { }
        foreach (var file in destination.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Layout-stride rejection leaked partial object/header changes.");
        Expect(SelectionFile.Serialize(selection).AsSpan().SequenceEqual(bytes), "Stride rejection changed the source buffer.");
    }
    var v1Path = Path.Combine(fixture.DirectoryPath, "v1-clipping.dkm");
    File.WriteAllBytes(v1Path, LegacySectionFile.Create(basis.Sections.Where(section => section.Signature != NativeMapDocument.LegacyEvents).Append(new(NativeMapDocument.LegacyEvents, 1, new byte[4]))).ToArray());
    var modern = NativeMapDocument.Load(path); modern.AddLegacyRecord("Creature", new(1, 1));
    var modernRoot = modern.EnsureScriptScope(modern.ScriptOwners().Single(owner => owner.Scope == 2)); modern.InsertScriptEvent(modernRoot.Owner, modernRoot.Field, timer);
    var historical = NativeMapDocument.Load(v1Path); var historicalBefore = historical.Files.ToDictionary(file => file, file => file.Data.ToArray());
    try { historical.PasteSelection(new(1, 1), modern.CopySelection(new(0, 0, 3, 3))); throw new Exception("Scripts were appended to a nested V1 scenario section."); } catch (InvalidOperationException) { }
    foreach (var file in historical.Files) Expect(file.Data.Span.SequenceEqual(historicalBefore[file]), "V1 script rejection leaked changes before explicit upgrade.");
}

void TestSelectionFiles()
{
    using var fixture = new TemporaryFixture();
    var document = NativeMapDocument.Create(30, 30, "SelectionFiles");
    var trigger = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Trigger"), new(0, 0));
    var action = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0));
    document.SetEventType(action, LegacyEventSchemas.All.Single(type => type.Name == "ACTION_ADDCREATURE"));
    action = document.Records().Single(record => record.File == action.File && record.Offset == action.Offset);
    document.SetField(action, action.Fields.Single(field => field.Name == "ChildAnd"), "60000");
    document.SetField(action, action.Fields.Single(field => field.Name == "Param5"), "1");
    document.SetField(action, action.Fields.Single(field => field.Name == "Param7"), "1"); // External coordinate: must not become local after file import.
    document.SetField(trigger, trigger.Fields.Single(field => field.Name == "ChildAnd"), action.Read(action.Fields.Single(field => field.Name == "ID")));
    var creature = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEKEEPER"), new(11, 12));
    document.SetField(creature, creature.Fields.Single(field => field.Name == "wKeeperTrigger"), trigger.Read(trigger.Fields.Single(field => field.Name == "ID")));
    foreach (var category in new[] { "GAMEOBJECT", "GAMETRAP", "GAMEDOOR", "GAMEHERO", "GAMENEUTRAL" })
        document.AddRecord(RecordTemplate.All.Single(item => item.Structure == category), new(12, 12));
    var gold = document.Records().Single(record => record.Category == "GAMEOBJECT");
    document.SetField(gold, gold.Fields.Single(field => field.Name == "dwObjectGoldValue"), uint.MaxValue.ToString());
    var rectangle = new TileRect(10, 10, 4, 4); var clipboard = document.CopySelection(rectangle);
    var bytes = SelectionFile.Serialize(clipboard);
    var parsed = SelectionFile.Parse(bytes);
    Expect(parsed.ObjectCount == 6 && parsed.ScriptCount == 2, "Enhanced DKZ lost objects or attached events.");
    Expect(SelectionFile.Serialize(parsed).AsSpan().SequenceEqual(bytes), "Enhanced DKZ did not round-trip exactly.");
    var path = Path.Combine(fixture.DirectoryPath, "selection.dkz");
    SelectionFile.SaveNew(path, clipboard);
    try { SelectionFile.SaveNew(path, clipboard); throw new Exception("Selection export overwrote an existing file."); }
    catch (IOException) { }
    Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes), "Refused selection overwrite changed the existing file.");
    Expect(!Directory.EnumerateFiles(fixture.DirectoryPath, "*.tmp").Any(), "Selection export leaked a staging file.");
    var destination = NativeMapDocument.Create(30, 30, "Destination");
    destination.PasteSelection(new(20, 20), SelectionFile.Read(path));
    var pasted = destination.Records().Single(record => record.Category == "GAMEKEEPER");
    Expect(pasted.Read(pasted.Fields.Single(field => field.Name == "dwKeeperLocationX")) == "21", "Imported selection used absolute object coordinates.");
    var pastedAction = destination.Records().Single(record => record.File.Signature == 0xD2 && record.Category == "Record 0xD6");
    Expect(pastedAction.Read(pastedAction.Fields.Single(field => field.Name == "ChildAnd")) == "60000", "DKZ treated reserved action bytes as a link or remapped them.");
    Expect(pastedAction.Read(pastedAction.Fields.Single(field => field.Name == "Param5")) == "1", "Imported selection moved an external script target.");
    Expect(NativeMapValidator.Validate(destination).All(item => item.Severity != DiagnosticSeverity.Error), "Imported DKZ has invalid script links.");
    destination.Undo(); Expect(!destination.Records().Any(record => record.File.Signature == 0xBE), "DKZ paste undo failed.");
    var container = LegacySectionFile.Parse(bytes);
    byte[] WithExtension(int version, byte[] payload)
    {
        var rebuilt = LegacySectionFile.Create(container.Sections.Select(section => section.Signature == SelectionFile.ClipboardSection ? new LegacySection(section.Signature, version, payload) : section)).ToArray();
        WriteUInt32(rebuilt, 0, LegacySectionFile.AreaFileSignature); return rebuilt;
    }
    var extension = container.Sections.Single(section => section.Signature == SelectionFile.ClipboardSection);
    var maskOffset = extension.Payload.Length - clipboard.Tiles.Tiles.Count * 5;
    var flagsOffset = maskOffset - clipboard.ObjectCount - clipboard.ScriptCount;
    var v1 = WithExtension(1, extension.Payload[..flagsOffset].ToArray());
    Expect(SelectionFile.Serialize(SelectionFile.Parse(v1)).AsSpan().SequenceEqual(bytes), "Version-one DKCP did not upgrade to equivalent provenance with exact payloads.");
    var v1Destination = NativeMapDocument.Create(30, 30, "OldExtension"); v1Destination.PasteSelection(new(20, 20), SelectionFile.Parse(v1));
    var oldAction = v1Destination.Records().Single(record => record.File.Signature == 0xD2 && record.Category == "Record 0xD6");
    Expect(oldAction.Read(oldAction.Fields.Single(field => field.Name == "Param5")) == "1", "Version-one DKCP import shifted an external coordinate.");
    foreach (var flag in new byte[] { 0, 1, 3, 255 })
    {
        var malformedPayload = extension.Payload.ToArray(); malformedPayload[flagsOffset] = flag;
        try { SelectionFile.Parse(WithExtension(3, malformedPayload)); throw new Exception("A missing, external or unknown placed-thing coordinate flag was accepted."); } catch (InvalidDataException) { }
    }
    // The sole action's coordinates are external: lying that they are local must fail.
    var invalidLocal = extension.Payload.ToArray(); invalidLocal[maskOffset - 1] = 2;
    try { SelectionFile.Parse(WithExtension(3, invalidLocal)); throw new Exception("An out-of-bounds local script point was accepted."); } catch (InvalidDataException) { }
    try { SelectionFile.Parse(WithExtension(2, extension.Payload[..(maskOffset - 1)].ToArray())); throw new Exception("Truncated coordinate provenance was accepted."); } catch (InvalidDataException) { }
    var v2 = SelectionFile.Parse(WithExtension(2, extension.Payload[..maskOffset].ToArray()));
    Expect(v2.ProtectedTileCount == 0 && SelectionFile.Serialize(v2).AsSpan().SequenceEqual(bytes), "Version-two clipboard compatibility failed.");
    try { SelectionFile.Parse(WithExtension(6, extension.Payload.ToArray())); throw new Exception("A future DKCP version was accepted."); } catch (InvalidDataException) { }
    var standardLength = 12 + container.Sections.Take(2).Sum(section => 12 + section.Payload.Length);
    var standard = bytes[..standardLength]; WriteUInt32(standard, 8, (uint)standard.Length - 12);
    var originalClipboard = SelectionFile.Parse(standard);
    Expect(originalClipboard.ObjectCount == 6 && originalClipboard.ScriptCount == 0, "Original DKZ compatibility failed.");
    var standardPath = Path.Combine(fixture.DirectoryPath, "original.dkz"); File.WriteAllBytes(standardPath, standard);
    var originalProject = NativeMapDocument.Load(standardPath); // The original KMAP/KTGS portion is also editable as a project.
    Expect(originalProject.Records().Count(record => record.Category == "Project.MAPTHING") == 6, "DKZ KTGS records are not compatible with the project reader.");
    try { NativeMapDocument.Load(path); throw new Exception("Enhanced DKZ opened as a project with stale extension data."); }
    catch (InvalidDataException) { }
    var converted = SelectionFile.ConvertToFormat(originalClipboard, false);
    destination.PasteSelection(new(20, 20), converted);
    Expect(destination.Records().Count(record => record.File.Signature == 0xBE) == 6, "Original DKZ did not convert to native objects.");
    var convertedGold = destination.Records().Single(record => record.Category == "GAMEOBJECT");
    Expect(convertedGold.Read(convertedGold.Fields.Single(field => field.Name == "dwObjectGoldValue")) == uint.MaxValue.ToString(), "DKZ conversion corrupted a signed union/unsigned gold value.");
    destination.Undo();
    var legacyClipboard = SelectionFile.ConvertToFormat(converted, true);
    var legacyBytes = SelectionFile.Serialize(legacyClipboard);
    Expect(SelectionFile.Parse(legacyBytes).ObjectCount == 6, "Project-compatible DKZ did not reopen.");
    try { SelectionFile.ConvertToFormat(parsed, true); throw new Exception("Cross-format selection silently lost scripts."); }
    catch (InvalidOperationException) { }
    var corrupt = (byte[])bytes.Clone(); var extensionStart = standardLength + 12;
    WriteUInt32(corrupt, extensionStart + 20, uint.MaxValue);
    try { SelectionFile.Parse(corrupt); throw new Exception("Malformed selection counts were accepted."); }
    catch (InvalidDataException) { }
    corrupt = (byte[])bytes.Clone(); corrupt[32 + 4 * 16 + 12 + 12] ^= 1; // Alter compatible KTGS while extension stays unchanged.
    try { SelectionFile.Parse(corrupt); throw new Exception("Inconsistent DKZ representations were accepted."); }
    catch (InvalidDataException) { }
    foreach (var length in new[] { 0, 11, 12, bytes.Length - 1 })
    {
        try { SelectionFile.Parse(bytes.AsSpan(0, length)); throw new Exception("Truncated DKZ was accepted."); }
        catch (InvalidDataException) { }
    }
}

void TestLegacyProjectEditing()
{
    Expect(LegacyProjectSchemas.Size("MAPTHING") == 64, "Editor MAPTHING layout is wrong.");
    Expect(LegacyProjectSchemas.Size("MTHBAND") == 832, "Editor hero band layout is wrong.");
    Expect(LegacyProjectSchemas.Size("TRIGGERLINK") == 40 && LegacyProjectSchemas.Size("TRIGGER") == 56, "Editor script layouts are wrong.");
    Expect(LegacyProjectSchemas.Size("MAPRECTEX") == 292 && LegacyProjectSchemas.Size("MAPPROPERTIES") == 14960, "Editor metadata layouts are wrong.");
    using var fixture = new TemporaryFixture();
    var sections = new List<(uint signature, uint version, byte[] bytes)>
    {
        (LegacySectionFile.TileSectionSignature, 1, new byte[8 + 20 * 20 * 4]),
        (NativeMapDocument.LegacyInfo, 7, new byte[LegacyProjectSchemas.Size("MAPPROPERTIES")]),
        (NativeMapDocument.LegacyPlayers, 3, new byte[7 * LegacyProjectSchemas.Size("MAPOPTIONS")]),
        (NativeMapDocument.LegacyThings, 6, new byte[64]),
        (NativeMapDocument.LegacyAreas, 1, new byte[292]),
        (NativeMapDocument.LegacyBands, 5, new byte[832]),
        (NativeMapDocument.LegacyEvents, 3, new byte[12 + 40 + 56 + 56 + 146 * 64]),
        (NativeMapDocument.LegacyGlobals, 1, new byte[4 + new[] { ("GLOBALTERRAIN", 38), ("GLOBALROOM", 24), ("GLOBALDOOR", 8), ("GLOBALTRAP", 15), ("GLOBALSPELL", 13), ("GLOBALCREATURE", 46), ("GLOBALCRSPELLS", 41) }.Sum(item => LegacyProjectSchemas.Size(item.Item1) * item.Item2)]),
        (0x12345678, 1, "UNKNOWN"u8.ToArray())
    };
    WriteUInt32(sections[0].bytes, 0, 20); WriteUInt32(sections[0].bytes, 4, 20);
    for (var index = 0; index < 400; index++) WriteUInt32(sections[0].bytes, 8 + index * 4, new MapTile(2, 2, 1).Packed);
    WriteUInt32(sections[1].bytes, 0, 20); WriteUInt32(sections[1].bytes, 4, 20);
    WriteUInt32(sections[3].bytes, 12, 5); WriteUInt32(sections[3].bytes, 16, 5);
    WriteUInt32(sections[6].bytes, 0, 1); WriteUInt32(sections[6].bytes, 4, 1); WriteUInt32(sections[6].bytes, 8, 1);
    BinaryPrimitives.WriteUInt16LittleEndian(sections[6].bytes.AsSpan(12 + 36), 1);
    BinaryPrimitives.WriteUInt16LittleEndian(sections[6].bytes.AsSpan(12 + 40 + 48), 1);
    BinaryPrimitives.WriteUInt16LittleEndian(sections[6].bytes.AsSpan(12 + 40 + 52), 2);
    BinaryPrimitives.WriteUInt16LittleEndian(sections[6].bytes.AsSpan(12 + 40 + 56 + 48), 2);
    sections[6].bytes[12 + 40 + 54] = LegacyEventSchemas.All.Single(type => type.Name == "TRIGGER_TIMEELAPSED").Type;
    sections[6].bytes[12 + 40 + 56 + 54] = LegacyEventSchemas.All.Single(type => type.Name == "ACTION_VICTORY").Type;
    WriteUInt32(sections[7].bytes, 0, 0x1FF1F);
    var data = new byte[12 + sections.Sum(section => 12 + section.bytes.Length)];
    WriteUInt32(data, 0, LegacySectionFile.MapFileSignature); WriteUInt32(data, 4, 1); WriteUInt32(data, 8, (uint)data.Length - 12);
    var offset = 12;
    foreach (var section in sections)
    {
        WriteUInt32(data, offset, section.signature); WriteUInt32(data, offset + 4, section.version); WriteUInt32(data, offset + 8, (uint)section.bytes.Length);
        section.bytes.CopyTo(data, offset + 12); offset += 12 + section.bytes.Length;
    }
    var path = Path.Combine(fixture.DirectoryPath, "Project.dkm"); File.WriteAllBytes(path, data);
    var document = NativeMapDocument.Load(path);
    Expect(document.IsLegacyProject && !document.HasChanges, "Loading a legacy project marked it dirty.");
    var unchanged = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "unchanged"));
    Expect(SaveProjectionChecks.BeforeEquals(document,data), "Unchanged legacy project was not byte-for-byte preserved.");
    var info = document.Records().Single(record => record.Category == "Project.MAPPROPERTIES");
    document.SetField(info, info.Fields.Single(field => field.Name == "Author"), "Éditeur");
    var spell = document.Records().First(record => record.Category == "Project.GLOBALCRSPELLS");
    document.SetField(spell, spell.Fields.Single(field => field.Name == "Range"), "25");
    document.Paint([new(5, 5)], new(6, 3, 1));
    var changed = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "changed"));
    var reopened = NativeMapDocument.Load(changed);
    var reopenedInfo = reopened.Records().Single(record => record.Category == "Project.MAPPROPERTIES");
    Expect(reopenedInfo.Read(reopenedInfo.Fields.Single(field => field.Name == "Author")) == "Éditeur", "Legacy Western ANSI text was corrupted.");
    Expect(reopened.GetTile(5, 5).Terrain == 6, "Legacy tile painting did not survive export.");
    Expect(LegacySectionFile.Read(changed).Sections.Single(section=>section.Signature==0x12345678).Version==1 && LegacySectionFile.Read(changed).Sections.Single(section=>section.Signature==0x12345678).Payload.Span.SequenceEqual("UNKNOWN"u8), "Unknown project section was lost.");
    document.Undo(); document.Undo(); document.Undo(); Expect(!document.HasChanges, "Legacy project undo failed to restore exact bytes.");
    var trigger = document.Records().Single(record => record.Category == "Project.TRIGGER");
    var duplicatedTrigger = document.DuplicateRecord(trigger);
    Expect(duplicatedTrigger.Read(duplicatedTrigger.Fields.Single(field => field.Name == "and")) != "2", "Duplicated legacy tree shares original child actions.");
    document.Undo(); Expect(!document.HasChanges, "Legacy script cloning undo did not restore original bytes.");
    foreach (var kind in document.LegacyAddChoices())
    {
        var added = document.AddLegacyRecord(kind, new(4, 5));
        var count = document.Records().Count;
        document.DuplicateRecord(added); Expect(document.Records().Count > count, kind + ": project duplication failed.");
        document.Undo();
        added = document.Records().Single(record => record.File == added.File && record.Offset == added.Offset);
        document.DeleteRecord(added); Expect(document.Records().Count == count - 1, kind + ": project deletion failed.");
        document.Undo(); document.Undo(); Expect(!document.HasChanges, kind + ": project record undo was not byte-for-byte reversible.");
    }
    document.Resize(24, 26, 2, 3, new(2, 2, 1));
    var resized = NativeMapDocument.Load(SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "resized")));
    Expect(resized.Width == 24 && resized.Height == 26, "Legacy resized map did not reopen.");
    var resizedThing = resized.Records().Single(record => record.File.Signature == NativeMapDocument.LegacyThings);
    Expect(resizedThing.Read(resizedThing.Fields.Single(field => field.Name == "x")) == "7", "Legacy resize did not shift a creature.");
    document.Undo(); Expect(!document.HasChanges, "Legacy resize undo did not restore all sections.");
    TestLegacyCompilation(document, fixture.DirectoryPath);
    var clipboardCreature = document.AddLegacyRecord("Creature", new(8, 8));
    var link = document.AddLegacyRecord("TriggerLink", new(0, 0));
    document.SetField(link, link.Fields.Single(field => field.Name == "node.type"), "2");
    document.SetField(link, link.Fields.Single(field => field.Name == "creature"), clipboardCreature.Read(clipboardCreature.Fields.Single(field => field.Name == "uniqueid")));
    document.SetField(link, link.Fields.Single(field => field.Name == "trigger"), "1");
    var beforeClipboard = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    var clip = document.CopySelection(new(7, 7, 3, 3));
    var diskClip = SelectionFile.Parse(SelectionFile.Serialize(clip));
    Expect(diskClip.ObjectCount == clip.ObjectCount && diskClip.ScriptCount == clip.ScriptCount, "Legacy DKZ lost scope links or scripts.");
    Expect(clip.ObjectCount == 1 && clip.ScriptCount == 3, "Legacy clipboard omitted a scope link or script graph.");
    document.PasteSelection(new(12, 12), diskClip);
    var pastedCreature = document.Records().Single(record => record.File.Signature == NativeMapDocument.LegacyThings && record.Read(record.Fields.Single(field => field.Name == "x")) == "13");
    var pastedUnique = pastedCreature.Read(pastedCreature.Fields.Single(field => field.Name == "uniqueid"));
    Expect(pastedUnique != clipboardCreature.Read(clipboardCreature.Fields.Single(field => field.Name == "uniqueid")), "Legacy pasted creature ID is shared.");
    var pastedLink = document.Records().Single(record => record.Category == "Project.TRIGGERLINK" && record.Read(record.Fields.Single(field => field.Name == "creature")) == pastedUnique);
    Expect(pastedLink.Read(pastedLink.Fields.Single(field => field.Name == "trigger")) != "1", "Legacy pasted scope shares original script state.");
    Expect(pastedLink.Read(pastedLink.Fields.Single(field => field.Name == "door")) == "0", "Legacy clipboard remapped a zero scope identifier.");
    document.Undo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(beforeClipboard[file]), "Legacy selection undo was not atomic.");
    document.CutSelection(new(7, 7, 3, 3), new(2, 2, 1));
    Expect(!document.Records().Any(record => record.Category == "Project.TRIGGERLINK" && record.Read(record.Fields.Single(field => field.Name == "creature")) != "0"), "Cut left a dangling entity scope link.");
    document.Undo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(beforeClipboard[file]), "Legacy cut did not restore entity scope links.");
    Expect(File.ReadAllBytes(path).AsSpan().SequenceEqual(data), "Legacy project source was modified.");
}

void TestLegacyCompilation(NativeMapDocument project, string directory)
{
    // This compiler fixture changes hundreds of independent scalar fields.
    // Request sufficient retained steps explicitly now that the editor uses
    // the source default of 200. Keep every existing byte-exact undo assertion;
    // default-depth truncation is tested independently in EditHistoryChecks.
    var previousHistoryDepth = project.HistoryDepth;
    project.SetHistoryDepth(NativeMapDocument.MaximumHistoryDepth);
    Console.WriteLine($"      compiler assignments: metadata={LegacyCompilerRules.Metadata.Count}, player={LegacyCompilerRules.Players.Count}");
    Expect(LegacyCompilerRules.Metadata.Count == 49 && LegacyCompilerRules.Players.Count == 91, "Compiler assignment extraction omitted fields.");
    var basePath = NativeMapDocument.Create(20, 20, "Basis").SaveCopy(Path.Combine(directory, "basis"));
    foreach (var definition in LegacyGlobalSchemas.All)
    {
        var count = LegacyCompilerRules.DefinitionOrder(definition.Group).Count;
        Expect(count > 0, "Empty original definition order: " + definition.Group);
        var bytes = new byte[56 + count * definition.RecordSize];
        WriteUInt32(bytes, 0, definition.Signature); WriteUInt32(bytes, 4, 4); WriteUInt32(bytes, 8, (uint)bytes.Length);
        WriteUInt32(bytes, 12, definition.Signature + 1); WriteUInt32(bytes, 16, 28); WriteUInt32(bytes, 20, (uint)count);
        WriteUInt32(bytes, 48, definition.Signature + 2); WriteUInt32(bytes, 52, (uint)bytes.Length - 56);
        if (definition.Group == "Creatures")
            for (var index = 0; index < count; index++) bytes[56 + index * definition.RecordSize + 0xF4B] = (byte)(index + 1);
        var fileName = definition.Group switch { "Terrains" => "Terrain", "Spells" => "KeeperSpells", "CrSpells" => "CreatureSpells", _ => definition.Group };
        File.WriteAllBytes(Path.Combine(directory, "basis", "Data", "Editor", fileName + ".kwd"), bytes);
    }
    var globalVariables = new byte[72];
    WriteUInt32(globalVariables, 0, 0xE6); WriteUInt32(globalVariables, 4, 4); WriteUInt32(globalVariables, 8, 72);
    WriteUInt32(globalVariables, 20, 1); WriteUInt32(globalVariables, 52, 16); WriteUInt32(globalVariables, 56, 0xEB); WriteUInt32(globalVariables, 60, 1234);
    File.WriteAllBytes(Path.Combine(directory, "basis", "Data", "Editor", "GlobalVariables.kwd"), globalVariables);
    var basis = NativeMapDocument.Load(basePath);
    var sourceBytes = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
    var baseBytes = basis.Files.ToDictionary(file => file, file => file.Data.ToArray());
    var info = project.Records().Single(record => record.Category == "Project.MAPPROPERTIES");
    var editCount = 0;
    void Edit(NativeRecord record, string fieldName, string value)
    { project.SetField(record, record.Fields.Single(field => field.Name == fieldName), value); editCount++; }
    foreach (var rule in LegacyCompilerRules.Metadata)
    {
        var field = info.Fields.Single(field => field.Name == rule.Source);
        Edit(info, rule.Source, field.Kind == NativeValueKind.WesternAnsi ? rule.Source == "Author" ? "Éditeur" : "Compiler test" : "2");
    }
    var playerOptions = project.Records().First(record => record.Category == "Project.MAPOPTIONS");
    foreach (var rule in LegacyCompilerRules.Players) Edit(playerOptions, rule.Source, "2");
    Edit(playerOptions, "PlayerName", "Élite");
    project.Paint(Enumerable.Range(0, 25).Select(index => new TilePoint(8 + index % 5, 8 + index / 5)), new(14, 5, 1)); editCount++;
    foreach (var kind in project.LegacyAddChoices().Where(kind => kind is not ("TriggerLink" or "Trigger" or "Action")))
    { project.AddLegacyRecord(kind, new(6, 7)); editCount++; }
    var trigger = project.Records().Single(record => record.Category == "Project.TRIGGER");
    project.SetEventType(trigger, LegacyEventSchemas.All.First(type => type.RecordSignature == 0xD5 && type.Type == 1));
    editCount++;
    var action = project.Records().Single(record => record.Category == "Project.ACTION");
    project.SetEventType(action, LegacyEventSchemas.All.First(type => type.RecordSignature == 0xD6 && type.Type == 10));
    editCount++;
    foreach (var definition in LegacyGlobalSchemas.All)
    {
        var structure = definition.Group switch { "Terrains" => "GLOBALTERRAIN", "Rooms" => "GLOBALROOM", "Doors" => "GLOBALDOOR", "Traps" => "GLOBALTRAP", "Spells" => "GLOBALSPELL", "Creatures" => "GLOBALCREATURE", _ => "GLOBALCRSPELLS" };
        var order = LegacyCompilerRules.DefinitionOrder(definition.Group);
        var record = project.Records().Where(record => record.Category == "Project." + structure).ElementAt(order.First(index => index >= 0));
        foreach (var field in definition.Fields) Edit(record, field.Name, field.BitMask == 0 ? "25" : "1");
    }
    var projectBytes = project.Files.ToDictionary(file => file, file => file.Data.ToArray());
    var compiled = project.CompileLegacyProject("Compiled", basis);
    Expect(LegacyCompilerRules.Elements("MapVars").Count > 100, "Map variable catalogue was not extracted.");
    Expect(!compiled.IsLegacyProject && compiled.Width == 20, "Compilation did not produce a native document.");
    var metadata = compiled.Records().Single(record => record.Category == "Map");
    Expect(metadata.Read(metadata.Fields.Single(field => field.Name == "wstrMainAuthor")) == "Éditeur", "Compilation lost Unicode metadata.");
    foreach (var rule in LegacyCompilerRules.Metadata)
        Expect(metadata.Read(metadata.Fields.Single(field => field.Name == rule.Target)) == info.Read(info.Fields.Single(field => field.Name == rule.Source)), "Metadata assignment was lost: " + rule.Source);
    var player = compiled.Records().First(record => record.Category == "GAMEPLAYER");
    foreach (var rule in LegacyCompilerRules.Players)
        Expect(player.Read(player.Fields.Single(field => field.Name == rule.Target)) == (rule.Source is "PlayerCameraX" or "PlayerCameraY" ? "0" : "2"), "AI/player assignment was lost: " + rule.Source);
    Expect(player.Read(player.Fields.Single(field => field.Name == "szPlayerName")) == "Élite", "ANSI player name was corrupted.");
    var activePlayers = compiled.Records().Where(record => record.Category == "GAMEPLAYER").Select(record => record.Read(record.Fields.Single(field => field.Name == "bGlobalPlayer")));
    Expect(activePlayers.SequenceEqual(new[] { "1", "2", "5" }), "Compilation misassigned non-contiguous active keepers.");
    Expect(compiled.Files.Single(file => file.Signature == 0xE6 && !file.ExportPath.Contains("Maps" + Path.DirectorySeparatorChar)).Data.Span.SequenceEqual(globalVariables), "Compilation changed shared global variables.");
    Expect(compiled.Files.Single(file => file.Signature == 0xE6 && file.ExportPath.Contains("Maps" + Path.DirectorySeparatorChar)).Length > 56, "Compilation did not write map-specific variables.");
    foreach (var structure in new[] { "GAMEOBJECT", "GAMETRAP", "GAMEDOOR", "GAMEKEEPER", "GAMEAREA", "GAMEGATE", "GAMEEFFECT", "GAMEBAND" })
        Expect(compiled.Records().Any(record => record.Category == structure), "Compilation lost an editing category: " + structure);
    foreach (var constant in LegacyCompilerRules.MetadataConstants) Expect(compiled.MainFile.Data.Span[constant.Offset] == constant.Value, "Original compiler metadata constant was lost.");
    foreach (var definition in LegacyGlobalSchemas.All)
    {
        var order = LegacyCompilerRules.DefinitionOrder(definition.Group);
        var first = order.Select((value, index) => (value, index)).First(item => item.value >= 0).index;
        var record = compiled.Records().Where(record => record.Category == "Definition." + definition.Group).ElementAt(first);
        foreach (var field in definition.Fields) Expect(record.Read(field) == (field.BitMask == 0 ? "25" : "1"), "Global assignment was lost: " + definition.Group + "." + field.Name);
    }
    Expect(compiled.Records().Count(record => record.File.Signature == 0xD2) == 2, "Compilation lost script records.");
    Expect(compiled.Records().Any(record => record.Category == "Definition.CrSpells" && record.Read(record.Fields.Single(field => field.Name == "Range")) == "25"), "Compilation ignored creature-spell changes.");
    var reopened = NativeMapDocument.Load(SaveProjectionChecks.SaveCopy(compiled, Path.Combine(directory, "compiled")));
    Expect(NativeMapValidator.Validate(reopened).All(item => item.Severity != DiagnosticSeverity.Error), "Compiled bundle did not reopen cleanly.");
    foreach (var file in project.Files) Expect(file.Data.Span.SequenceEqual(projectBytes[file]), "Compilation changed the source project.");
    foreach (var file in basis.Files) Expect(file.Data.Span.SequenceEqual(baseBytes[file]), "Compilation changed the base definitions.");
    for (var index = 0; index < editCount; index++) project.Undo();
    foreach (var file in project.Files) Expect(file.Data.Span.SequenceEqual(sourceBytes[file]), "Compiler test did not undo its fixture changes.");
    project.SetHistoryDepth(previousHistoryDepth);
}

void TestResize()
{
    using var fixture = new TemporaryFixture();
    var document = NativeMapDocument.Create(20, 20, "ResizeTest");
    document.Paint([new(5, 5)], new(6, 3, 2));
    var creature = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEKEEPER"), new(5, 5));
    var action = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0));
    document.SetEventType(action, LegacyEventSchemas.All.Single(item => item.Name == "ACTION_ADDCREATURE"));
    action = document.Records().Single(item => item.File == action.File && item.Offset == action.Offset);
    document.SetField(action, action.Fields.Single(field => field.Name == "Param5"), "6");
    document.SetField(action, action.Fields.Single(field => field.Name == "Param7"), "7");
    var before = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    document.Resize(24, 26, 2, 3, new(2, 2, 1));
    Expect(document.Width == 24 && document.Height == 26, "Resize dimensions are wrong.");
    Expect(document.GetTile(7, 8) == new MapTile(6, 3, 2), "Resize lost or moved the wrong tile.");
    Expect(document.GetTile(0, 0).Terrain == 30 && document.GetTile(23, 25).Terrain == 30, "Resize failed to rebuild the map boundary.");
    Expect(creature.Read(creature.Fields.Single(field => field.Name.EndsWith("LocationX", StringComparison.Ordinal))) == "7", "Creature X was not shifted.");
    Expect(creature.Read(creature.Fields.Single(field => field.Name.EndsWith("LocationY", StringComparison.Ordinal))) == "8", "Creature Y was not shifted.");
    Expect(action.Read(action.Fields.Single(field => field.Name == "Param5")) == "8" && action.Read(action.Fields.Single(field => field.Name == "Param7")) == "10", "Scripted coordinates were not shifted.");
    var exported = SaveProjectionChecks.SaveCopy(document, Path.Combine(fixture.DirectoryPath, "resize"));
    Expect(NativeMapDocument.Load(exported).Width == 24, "Resized export did not reopen.");
    document.Undo();
    foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Resize undo was not byte-for-byte atomic.");
    try { document.Resize(4, 4, 0, 0, new(2, 2, 1)); throw new Exception("Resize silently cropped a creature."); }
    catch (InvalidOperationException) { }
    foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Rejected crop did not roll back.");
    try { document.Resize(4, 4, 0, 0, new(2, 2, 1), true); throw new Exception("Resize accepted an invalid script target."); }
    catch (InvalidOperationException) { }
    foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(before[file]), "Rejected scripted crop did not roll back.");
    document.DeleteRecord(action);
    document.Resize(4, 4, 0, 0, new(2, 2, 1), true);
    Expect(!document.Records().Any(item => item.Category == "GAMEKEEPER"), "Explicit cropping did not remove the creature.");
    document.Undo();
    Expect(document.Width == 20 && document.Records().Any(item => item.Category == "GAMEKEEPER"), "Crop undo failed to restore the creature and dimensions.");
}

void TestLegacySections()
{
    var data = new byte[12 + 12 + 8 + 4 * 6 + 12 + 7];
    WriteUInt32(data, 0, LegacySectionFile.MapFileSignature); WriteUInt32(data, 4, 1); WriteUInt32(data, 8, (uint)data.Length - 12);
    WriteUInt32(data, 12, LegacySectionFile.TileSectionSignature); WriteUInt32(data, 16, 1); WriteUInt32(data, 20, 32);
    WriteUInt32(data, 24, 3); WriteUInt32(data, 28, 2);
    for (var index = 0; index < 6; index++) WriteUInt32(data, 32 + index * 4, new MapTile((byte)(index + 1), 2, 1).Packed);
    WriteUInt32(data, 56, 0x12345678); WriteUInt32(data, 60, 1); WriteUInt32(data, 64, 7);
    "UNKNOWN"u8.CopyTo(data.AsSpan(68));
    var legacy = LegacySectionFile.Parse(data);
    Expect(legacy.ToArray().AsSpan().SequenceEqual(data), "Unchanged legacy sections did not round-trip.");
    var tiles = legacy.ReadTiles(); Expect(tiles.Width == 3 && tiles.Height == 2, "Legacy dimensions are wrong.");
    legacy.ReplaceTiles(tiles.RotateClockwise());
    var edited = LegacySectionFile.Parse(legacy.ToArray());
    Expect(edited.ReadTiles().Width == 2, "Legacy tile edit did not survive serialization.");
    Expect(edited.Sections[1].Payload.Span.SequenceEqual("UNKNOWN"u8), "Editing tiles changed an unknown section.");
    WriteUInt32(data, 20, uint.MaxValue);
    try { LegacySectionFile.Parse(data); throw new Exception("An oversized legacy section was accepted."); }
    catch (InvalidDataException) { }
}

void TestScriptCloning()
{
    var document = NativeMapDocument.Create(20, 20, "CloneEvents");
    var trigger = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Trigger"), new(0, 0));
    var action = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0));
    document.SetField(trigger, trigger.Fields.Single(field => field.Name == "ChildAnd"), action.Read(action.Fields.Single(field => field.Name == "ID")));
    var reservedValue = trigger.Read(trigger.Fields.Single(field => field.Name == "ID"));
    document.SetField(action, action.Fields.Single(field => field.Name == "ChildAnd"), reservedValue);
    var creature = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "GAMEKEEPER"), new(5, 5));
    var creatureTrigger = creature.Fields.Single(field => field.Name == "wKeeperTrigger");
    document.SetField(creature, creatureTrigger, trigger.Read(trigger.Fields.Single(field => field.Name == "ID")));
    var files = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    var duplicate = document.DuplicateRecord(creature);
    Expect(duplicate.Read(creatureTrigger) != creature.Read(creatureTrigger), "Duplicated creature shares script state with the original.");
    var clonedTrigger = document.Records().Single(item => item.File.Signature == 0xD2 && item.Read(item.Fields.Single(field => field.Name == "ID")) == duplicate.Read(creatureTrigger));
    var child = clonedTrigger.Read(clonedTrigger.Fields.Single(field => field.Name == "ChildAnd"));
    Expect(child != action.Read(action.Fields.Single(field => field.Name == "ID")), "Cloned script graph still links to original actions.");
    var clonedAction = document.Records().Single(item => item.File.Signature == 0xD2 && item.Read(item.Fields.Single(field => field.Name == "ID")) == child);
    Expect(clonedAction.Read(clonedAction.Fields.Single(field => field.Name == "ChildAnd")) == reservedValue, "Cloning remapped reserved native action bytes.");
    Expect(NativeMapValidator.Validate(document).All(item => item.Severity != DiagnosticSeverity.Error), "Script clone produced validation errors.");
    document.Undo();
    foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(files[file]), "Entity plus script undo was not atomic.");
    document.Redo();
    document.DeleteRecord(document.Records().Single(item => item.File == duplicate.File && item.Offset == duplicate.Offset));
    Expect(document.Records().Count(item => item.Category == "GAMEKEEPER") == 1, "Scripted-map creature deletion was incorrectly disabled.");
    var clipboard = document.CopySelection(new(5, 5, 1, 1));
    Expect(clipboard.ScriptCount == 2, "Copy followed reserved native action bytes as graph links.");
    files = document.Files.ToDictionary(file => file, file => file.Data.ToArray());
    document.PasteSelection(new(8, 8), clipboard);
    var pasted = document.Records().Single(item => item.Category == "GAMEKEEPER" && item.Read(item.Fields.Single(field => field.Name == "dwKeeperLocationX")) == "8");
    var graph = ScriptGraph.Build(document); var branch = graph.Reachable(ushort.Parse(pasted.Read(creatureTrigger)));
    Expect(branch.Count == 2 && branch.Select(id => graph.Events[id]).Single(item => item.Type?.RecordSignature == 0xD6).Record.Read(action.Fields.Single(field => field.Name == "ChildAnd")) == reservedValue,
        "Pasting remapped reserved native action bytes or followed them as links.");
    document.Undo(); foreach (var file in document.Files) Expect(file.Data.Span.SequenceEqual(files[file]), "Reserved-byte clipboard test undo changed original data.");

    var unlinked = NativeMapDocument.Create(8, 8, "ReservedDelete");
    var unlinkedCondition = unlinked.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Trigger"), new(0, 0));
    var unlinkedAction = unlinked.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0));
    unlinked.SetField(unlinkedAction, unlinkedAction.Fields.Single(field => field.Name == "ChildAnd"), ScriptGraph.ID(unlinkedCondition).ToString());
    unlinked.DeleteRecord(unlinkedCondition);
    Expect(ScriptGraph.Build(unlinked).Events.Count == 1, "Reserved native action bytes incorrectly blocked deleting an unreferenced event.");
}

void TestEvents()
{
    var types = LegacyEventSchemas.All;
    Console.WriteLine($"      conditions={types.Count(type => type.RecordSignature == 0xD5)} actions={types.Count(type => type.RecordSignature == 0xD6)}");
    Expect(types.Count >= 90, "The event catalogue is unexpectedly small.");
    var variable = types.Single(type => type.Name == "TRIGGER_VARIABLE");
    Expect(variable.Arguments.Single(field => field.Name == "Param5").Offset == 12, "Variable comparison layout is wrong.");
    var spawn = types.Single(type => type.Name == "ACTION_ADDCREATURE");
    Expect(spawn.Arguments.Single(field => field.Name == "Param7").Offset == 14, "Spawn Y coordinate layout is wrong.");
    var document = NativeMapDocument.Create(20, 20, "Events");
    foreach (var type in types)
    {
        var template = RecordTemplate.All.Single(item => item.Structure == (type.RecordSignature == 0xD5 ? "Trigger" : "Action"));
        var record = document.AddRecord(template, new(0, 0));
        document.SetEventType(record, type);
        var edited = document.Records().Single(item => item.File == record.File && item.Offset == record.Offset);
        foreach (var argument in type.Arguments)
        {
            Expect(argument.Offset >= 8 && argument.Offset + argument.Size <= 16, type.Name + ": argument escapes payload.");
            document.SetField(edited, argument, "1");
            Expect(edited.Read(argument) == "1", type.Name + ": typed argument did not round-trip.");
        }
        document.DeleteRecord(edited);
    }
    Expect(!document.Records().Any(record => record.File.Signature == 0xD2), "Deleting event variants left records behind.");
    var trigger = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Trigger"), new(0, 0));
    var action = document.AddRecord(RecordTemplate.All.Single(item => item.Structure == "Action"), new(0, 0));
    var child = trigger.Fields.Single(field => field.Name == "ChildAnd");
    var actionID = action.Fields.Single(field => field.Name == "ID");
    document.SetField(trigger, child, action.Read(actionID));
    var reserved = action.Fields.Single(field => field.Name == "ChildAnd");
    var originalID = action.Read(actionID);
    document.SetField(action, reserved, originalID);
    document.SetField(action, actionID, "300");
    Expect(trigger.Read(child) == "300", "Changing an event ID did not update incoming links.");
    Expect(action.Read(reserved) == originalID, "Changing an event ID rewrote reserved native action bytes.");
    document.Undo(); Expect(trigger.Read(child) == action.Read(actionID), "Event ID remapping undo failed.");
    var beforeRejectedID = action.File.Data.ToArray();
    try { document.SetField(action, actionID, trigger.Read(trigger.Fields.Single(field => field.Name == "ID"))); throw new Exception("Duplicate event ID was accepted."); }
    catch (ArgumentException) { }
    Expect(action.File.Data.Span.SequenceEqual(beforeRejectedID), "Rejected ID change altered script data.");
}

void TestGlobalSchemas()
{
    Expect(LegacyGlobalSchemas.All.Count == 7, "Seven global definition families are required.");
    Console.WriteLine("      global fields: " + string.Join(", ", LegacyGlobalSchemas.All.Select(definition => definition.Group + "=" + definition.Fields.Count)));
    Expect(LegacyGlobalSchemas.All.Sum(definition => definition.Fields.Count) >= 260, "Global schema extraction missed fields.");
    var creatures = LegacyGlobalSchemas.Find(0xAA)!;
    Expect(creatures.Fields.Single(field => field.Name == "Health").Offset == 0xF0C, "Creature health offset is wrong.");
    Expect(creatures.Fields.Single(field => field.Name == "HJobs[0]").Offset == 0xE14, "Happy job offset is wrong.");
    Expect(creatures.Fields.Single(field => field.Name == "CanBePicked").BitMask == 2, "Creature flag mask is wrong.");
    var terrain = LegacyGlobalSchemas.Find(0x6E)!;
    Expect(terrain.Fields.Single(field => field.Name == "Visible").BitMask == 1, "Terrain ternary flag was not parsed.");
    var signedDamage = terrain.Fields.Single(field => field.Name == "Damage");
    Expect(signedDamage.Kind == NativeValueKind.Int16, "Signed terrain damage is wrong.");
}

void ValidateExternalPath(string path)
{
    var files = Directory.Exists(path)
        ? Directory.EnumerateFiles(path, "*.kwd", SearchOption.AllDirectories)
        : [path];

    foreach (var file in files)
    {
        var result = inspector.Inspect(file);
        var errors = result.Diagnostics.Count(item => item.Severity == DiagnosticSeverity.Error);
        var warnings = result.Diagnostics.Count(item => item.Severity == DiagnosticSeverity.Warning);
        Console.WriteLine($"MAP   {Path.GetFileName(file)}  {result.Width}x{result.Height}  errors={errors} warnings={warnings}");
        if (errors > 0)
        {
            failures.Add($"real map {file}: {errors} structural error(s): " + string.Join("; ",result.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error).Take(5).Select(item => item.Message)));
        }
        if (Path.GetExtension(file).Equals(".map", StringComparison.OrdinalIgnoreCase))
        {
            Run($"text import / DKM / native compile {Path.GetFileName(file)}", () =>
            {
                using var fixture = new TemporaryFixture();
                var original = File.ReadAllBytes(file); var project = NativeMapDocument.Load(file);
                var scenarioNames = ScenarioNameChecks.ReadOnlyProjection(project);
                var levelOptions = MapLevelOptionsChecks.ReadOnlyProjection(project);
                var speechText = MapSpeechTextChecks.ReadOnlyProjection(project);
                var general = MapGeneralSettingsChecks.ReadOnlyProjection(project);
                var gameVersion = GameVersionCompatibilityChecks.ReadOnlyProjection(project);
                var ruleBytes = project.Files.ToDictionary(item => item, item => item.Data.ToArray());
                var ruleCells = project.MapRuleCells();
                var variableCells = project.MapVariableCells();
                var playerCells = project.PlayerSettingsRows();
                var geometryCells = project.PlayerGeometries();
                var statistics = MapStatisticsChecks.Projection(project);
                foreach (var item in project.Files) Expect(item.Data.Span.SequenceEqual(ruleBytes[item]), "Matrix projection changed imported project bytes.");
                var saved = SaveProjectionChecks.SaveCopy(project, Path.Combine(fixture.DirectoryPath, "project")); var reopened = NativeMapDocument.Load(saved);
                Expect(scenarioNames == ScenarioNameChecks.ReadOnlyProjection(reopened), "Project scenario names/choices changed after reopening.");
                Expect(levelOptions == MapLevelOptionsChecks.ReadOnlyProjection(reopened), "Project level options changed after reopening.");
                Expect(speechText == MapSpeechTextChecks.ReadOnlyProjection(reopened), "Project speech/text metadata changed after reopening.");
                Expect(general == MapGeneralSettingsChecks.ReadOnlyProjection(reopened), "Project general settings changed after reopening.");
                Expect(gameVersion == GameVersionCompatibilityChecks.ReadOnlyProjection(reopened), "Project compatibility notices changed after reopening.");
                Expect(statistics == MapStatisticsChecks.Projection(reopened), "Project statistics changed after reopening.");
                Expect(ruleCells.Select(cell => (cell.Key, cell.Value)).SequenceEqual(reopened.MapRuleCells().Select(cell => (cell.Key, cell.Value))), "Project matrix projection changed after reopening.");
                Expect(variableCells.Select(cell => (cell.Definition.ID, cell.Bits, cell.RecordCount)).SequenceEqual(reopened.MapVariableCells().Select(cell => (cell.Definition.ID, cell.Bits, cell.RecordCount))), "Project variable projection changed after reopening.");
                Expect(playerCells.Select(row => (row.ID, row.Name, row.Gold, row.Computer, row.Health, row.AI)).SequenceEqual(reopened.PlayerSettingsRows().Select(row => (row.ID, row.Name, row.Gold, row.Computer, row.Health, row.AI))), "Project player projection changed after reopening.");
                Expect(geometryCells.Select(row => (row.ID, row.Camera, row.BuildStart, row.BuildEnd, row.EffectiveCamera)).SequenceEqual(reopened.PlayerGeometries().Select(row => (row.ID, row.Camera, row.BuildStart, row.BuildEnd, row.EffectiveCamera))), "Project geometry projection changed after reopening.");
                SaveProjectionChecks.RoundTrip(project,reopened,ruleBytes.ToDictionary(pair=>pair.Key.ExportPath,pair=>pair.Value));
                var rawBefore=project.Records();var rawAfter=reopened.Records();
                Console.WriteLine($"REAL MAP RECORDS raw-before={rawBefore.Count} raw-after={rawAfter.Count} KSTS-before={rawBefore.Count(record=>record.File.Signature==ActivityStatisticsPayload.Signature)} KSTS-after={rawAfter.Count(record=>record.File.Signature==ActivityStatisticsPayload.Signature)} exact-full-source-projection");
                var before = rawBefore.Where(record=>record.File.Signature!=ActivityStatisticsPayload.Signature).ToArray();
                var after = rawAfter.Where(record=>record.File.Signature!=ActivityStatisticsPayload.Signature).ToArray();
                Expect(before.Length == after.Length, "Saving imported MAP lost records.");
                for (var i = 0; i < before.Length; i++) Expect(before[i].Category == after[i].Category && before[i].File.Data.Span.Slice(before[i].Offset, before[i].Length).SequenceEqual(after[i].File.Data.Span.Slice(after[i].Offset, after[i].Length)), "Saving changed imported record data.");
                Expect(project.TileFile.Data.Span.SequenceEqual(reopened.TileFile.Data.Span), "Saving changed imported tiles.");
                var compiled = project.CompileLegacyProject("ImportedText", NativeMapDocument.Create(project.Width, project.Height, "Basis"));
                var native = NativeMapDocument.Load(SaveProjectionChecks.SaveCopy(compiled, Path.Combine(fixture.DirectoryPath, "native")));
                Expect(native.TileFile.Data.Span.SequenceEqual(project.TileFile.Data.Span), "Native compilation changed imported tiles.");
                Expect(native.Records().Count(record => record.File.Signature == 0xD2) == before.Count(record => record.Category is "Project.TRIGGER" or "Project.ACTION"), "Native compilation lost text scenario records.");
                Expect(File.ReadAllBytes(file).SequenceEqual(original), "Import mutated source text.");
                var diagnostics = NativeMapValidator.Validate(project);
                Expect(!diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error), "Text import has semantic errors.");
                var terrain = TerrainLayoutDiagnostics.Validate(project);
                Expect(terrain.Select(item => (item.Code, item.Severity, item.Region)).SequenceEqual(TerrainLayoutDiagnostics.Validate(native).Select(item => (item.Code, item.Severity, item.Region))), "Compilation changed terrain diagnostics.");
                Console.WriteLine($"      terrain errors={terrain.Count(item => item.Severity == DiagnosticSeverity.Error)} warnings={terrain.Count(item => item.Severity == DiagnosticSeverity.Warning)}");
                var textCopy = SaveProjectionChecks.ExportText(Path.Combine(fixture.DirectoryPath, "text-copy"), project);
                var exactProject = NativeMapDocument.Load(textCopy.EditableCopyPath);
                Expect(SaveProjectionChecks.PreviousEquals(project,File.ReadAllBytes(saved)), "Complete text-export snapshot lost project bytes.");
                var classic = NativeMapDocument.Load(textCopy.TextPath);
                Expect(classic.Records().Count(record => record.Category == "Project.TRIGGER") == before.Count(record => record.Category == "Project.TRIGGER") && classic.Records().Count(record => record.Category == "Project.ACTION") == before.Count(record => record.Category == "Project.ACTION"), "Text export lost text-compatible scenario records.");
                Console.WriteLine($"      text objects={before.Count(record => record.Category == "Project.MAPTHING")} bands={before.Count(record => record.Category == "Project.MTHBAND")} conditions={before.Count(record => record.Category == "Project.TRIGGER")} actions={before.Count(record => record.Category == "Project.ACTION")}");
            });
            continue;
        }
        Run($"native round-trip {file}", () =>
        {
            using var fixture = new TemporaryFixture();
            var native = NativeMapDocument.Load(file);
            var snapshots = native.Files.ToDictionary(item => item.ExportPath, item => item.Data.ToArray());
            var unusedScriptPreview = UnusedScriptChecks.ReadOnlyProjection(native);
            var scenarioNames = ScenarioNameChecks.ReadOnlyProjection(native);
            var levelOptions = MapLevelOptionsChecks.ReadOnlyProjection(native);
            var speechText = MapSpeechTextChecks.ReadOnlyProjection(native);
            var general = MapGeneralSettingsChecks.ReadOnlyProjection(native);
            var gameVersion = GameVersionCompatibilityChecks.ReadOnlyProjection(native);
            var statistics = MapStatisticsChecks.Projection(native);
            var cameraPaths = CameraPathChecks.Projection(native);
            var fullmap = FullMapOverview.Capture(native);
            var thumbnailOptions = new MapThumbnailOptions(WorldFlags: 0x101,PlayersFlags: 0x8ABCDE00,HiddenColour: 0x123456,UseColourKey: true);
            var thumbnail = MapThumbnail.Bmp(native,thumbnailOptions);
            var exported = SaveProjectionChecks.SaveCopy(native, Path.Combine(fixture.DirectoryPath, "game-root"));
            var reopened = NativeMapDocument.Load(exported);
            var reopenedFullmap = FullMapOverview.Capture(reopened);
            Expect(unusedScriptPreview == UnusedScriptChecks.ReadOnlyProjection(reopened), "Unused-event preview changed after native reopening.");
            Expect(scenarioNames == ScenarioNameChecks.ReadOnlyProjection(reopened), "Native scenario names/choices changed after reopening.");
            Expect(levelOptions == MapLevelOptionsChecks.ReadOnlyProjection(reopened), "Native level options changed after reopening.");
            Expect(speechText == MapSpeechTextChecks.ReadOnlyProjection(reopened), "Native speech/text metadata changed after reopening.");
            Expect(general == MapGeneralSettingsChecks.ReadOnlyProjection(reopened), "Native general settings changed after reopening.");
            Expect(gameVersion == GameVersionCompatibilityChecks.ReadOnlyProjection(reopened), "Native compatibility notices changed after reopening.");
            Expect(cameraPaths == CameraPathChecks.Projection(reopened), "Camera action projection changed after reopening.");
            Expect(thumbnail.AsSpan().SequenceEqual(MapThumbnail.Bmp(reopened,thumbnailOptions)),"Native thumbnail projection changed after reopening.");
            Expect(fullmap.NeutralCells.SequenceEqual(reopenedFullmap.NeutralCells) && fullmap.Markers.SequenceEqual(reopenedFullmap.Markers),"Native fullscreen projection changed after reopening.");
            Expect(statistics == MapStatisticsChecks.Projection(reopened), "Native statistics changed after reopening.");
            Expect(reopened.Files.Count == native.Files.Count, "Export lost referenced files.");
            foreach (var item in reopened.Files) Expect(item.Data.Span.SequenceEqual(snapshots[item.ExportPath]), $"Round-trip changed {item.ExportPath}");
            var fieldRecord = native.Records().First(item => item.Category == "Map");
            var name = fieldRecord.Fields.Single(item => item.Name == "wstrMainName");
            native.SetField(fieldRecord, name, "Проверка Unicode / Test");
            Expect(fieldRecord.Read(name) == "Проверка Unicode / Test", "Unicode metadata edit failed.");
            native.Undo();
            Expect(!native.HasChanges, "Metadata undo did not recover original bytes.");
            var undoneFullmap = FullMapOverview.Capture(native);
            Expect(unusedScriptPreview == UnusedScriptChecks.ReadOnlyProjection(native), "Unused-event preview changed after metadata undo.");
            Expect(scenarioNames == ScenarioNameChecks.ReadOnlyProjection(native), "Native scenario names/choices changed after metadata undo.");
            Expect(levelOptions == MapLevelOptionsChecks.ReadOnlyProjection(native), "Native level options changed after metadata undo.");
            Expect(speechText == MapSpeechTextChecks.ReadOnlyProjection(native), "Native speech/text metadata changed after metadata undo.");
            Expect(general == MapGeneralSettingsChecks.ReadOnlyProjection(native), "Native general settings changed after metadata undo.");
            Expect(gameVersion == GameVersionCompatibilityChecks.ReadOnlyProjection(native), "Native compatibility notices changed after metadata undo.");
            Expect(cameraPaths == CameraPathChecks.Projection(native), "Camera action projection changed after metadata undo.");
            Expect(thumbnail.AsSpan().SequenceEqual(MapThumbnail.Bmp(native,thumbnailOptions)),"Native thumbnail projection changed after metadata undo.");
            Expect(fullmap.NeutralCells.SequenceEqual(undoneFullmap.NeutralCells) && fullmap.Markers.SequenceEqual(undoneFullmap.Markers),"Native fullscreen projection changed after metadata undo.");
            var definition = native.Records().FirstOrDefault(item => item.Category == "Definition.Terrains" && item.Fields.Any(field => field.Name == "Visible"));
            if (definition is not null)
            {
                var flag = definition.Fields.Single(field => field.Name == "Visible");
                var originalFlagByte = definition.File.Data.Span[definition.Offset + flag.Offset];
                native.SetField(definition, flag, definition.Read(flag) == "0" ? "1" : "0");
                var changedFlagByte = definition.File.Data.Span[definition.Offset + flag.Offset];
                Expect((originalFlagByte ^ changedFlagByte) == flag.BitMask, "Flag edit changed unrelated bits.");
                var scopedPath = SaveProjectionChecks.SaveCopy(native, Path.Combine(fixture.DirectoryPath, "scoped-definition"));
                var scoped = NativeMapDocument.Load(scopedPath);
                var exportedDefinition = scoped.Records().First(item => item.Category == "Definition.Terrains");
                Expect(exportedDefinition.Read(flag) == definition.Read(flag), "Scoped override did not reopen correctly.");
                if (!definition.File.ExportPath.Contains("Maps" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    Expect(exportedDefinition.File.ExportPath.Contains("Maps" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Shared definitions were not scoped to this map.");
                native.Undo(); Expect(!native.HasChanges, "Global flag undo did not restore the original.");
            }
            var nativeDiagnostics = NativeMapValidator.Validate(native);
            RandomPaintChecks.CheckLoaded(native);
            ObjectBatchPlacementChecks.CheckLoaded(native);
            ObjectManagerChecks.CheckLoaded(native);
            ObjectModificationChecks.CheckLoaded(native);
            ObjectPaintingChecks.CheckLoaded(native);
            Console.WriteLine($"      semantic errors={nativeDiagnostics.Count(item => item.Severity == DiagnosticSeverity.Error)} warnings={nativeDiagnostics.Count(item => item.Severity == DiagnosticSeverity.Warning)}");
            var terrain = TerrainLayoutDiagnostics.Validate(native);
            Expect(terrain.Select(item => (item.Code, item.Severity, item.Region)).SequenceEqual(TerrainLayoutDiagnostics.Validate(reopened).Select(item => (item.Code, item.Severity, item.Region))), "Native round-trip changed terrain diagnostics.");
            Expect(native.MapRuleCells().Select(cell => (cell.Key, cell.Value, cell.RecordCount)).SequenceEqual(reopened.MapRuleCells().Select(cell => (cell.Key, cell.Value, cell.RecordCount))), "Native matrix projection changed after reopening.");
            Expect(native.MapVariableCells().Select(cell => (cell.Definition.ID, cell.Bits, cell.RecordCount)).SequenceEqual(reopened.MapVariableCells().Select(cell => (cell.Definition.ID, cell.Bits, cell.RecordCount))), "Native variable projection changed after reopening.");
            Expect(native.PlayerSettingsRows().Select(row => (row.ID, row.Name, row.Gold, row.Computer, row.Health, row.AI)).SequenceEqual(reopened.PlayerSettingsRows().Select(row => (row.ID, row.Name, row.Gold, row.Computer, row.Health, row.AI))), "Native player projection changed after reopening.");
            Expect(native.PlayerGeometries().Select(row => (row.ID, row.Camera, row.BuildStart, row.BuildEnd)).SequenceEqual(reopened.PlayerGeometries().Select(row => (row.ID, row.Camera, row.BuildStart, row.BuildEnd))), "Native geometry projection changed after reopening.");
            foreach (var item in native.Files) Expect(item.Data.Span.SequenceEqual(snapshots[item.ExportPath]), "Diagnostics or matrix projection changed source buffers.");
            var newMap = NativeMapDocument.CreateNewMap(new NewMapOptions("CorpusNew", "New map", 32, 20, 2, PlaceHearts: true, NativeFormat: true), native);
            NativeDefinitionReuseChecks.Verify(native,newMap);
            foreach (var item in native.Files) Expect(item.Data.Span.SequenceEqual(snapshots[item.ExportPath]), "New-map factory mutated its source map/definitions.");
            Console.WriteLine($"      terrain errors={terrain.Count(item => item.Severity == DiagnosticSeverity.Error)} warnings={terrain.Count(item => item.Severity == DiagnosticSeverity.Warning)}");
            Console.WriteLine($"      files={native.Files.Count} records={native.Records().Count} fields={native.Records().Sum(item => item.Fields.Count)} missing={native.MissingReferences.Count}");
        });
        // Native preservation must be tested even when the separate historical
        // text projection refuses imported script/format validation errors.
        // Keep that refusal visible as its own failing check, never swallow it
        // or call the map's native round-trip successful based on text alone.
        Run($"text projection {file}", () =>
        {
            using var fixture = new TemporaryFixture(); var native = NativeMapDocument.Load(file);
            var snapshots = native.Files.ToDictionary(item => item.ExportPath,item => item.Data.ToArray());
            var textCopy = SaveProjectionChecks.ExportText(Path.Combine(fixture.DirectoryPath,"text-copy"),native);
            var exactTextCopy = NativeMapDocument.Load(textCopy.EditableCopyPath);
            foreach (var item in exactTextCopy.Files) Expect(item.Data.Span.SequenceEqual(snapshots[item.ExportPath]),$"Text snapshot changed {item.ExportPath}");
            Expect(inspector.Inspect(textCopy.TextPath).IsValid,"Classic text projection did not reopen.");
            foreach (var item in native.Files) Expect(item.Data.Span.SequenceEqual(snapshots[item.ExportPath]),"Text projection mutated source buffers.");
        });
        // Explicit original-editor scenario policy is a separate acceptance
        // scope. Keep failures of the supported-script workflow above visible.
        Run($"original text projection {file}", () =>
        {
            using var fixture = new TemporaryFixture(); var native = NativeMapDocument.Load(file);
            var snapshots = native.Files.ToDictionary(item => item.ExportPath,item => item.Data.ToArray());
            var textCopy = SaveProjectionChecks.ExportText(Path.Combine(fixture.DirectoryPath,"original-text-copy"),native,TextMapScriptMode.OriginalNoScenarios);
            var exact = NativeMapDocument.Load(textCopy.EditableCopyPath);
            foreach (var item in exact.Files) Expect(item.Data.Span.SequenceEqual(snapshots[item.ExportPath]),$"Original-mode snapshot changed {item.ExportPath}");
            var projected = NativeMapDocument.Load(textCopy.TextPath);
            Expect(!projected.Records().Any(ScriptGraph.IsEvent) && ScriptGraph.Build(projected).Roots.All(edge => edge.Target == 0),"Original MAP projection retained scenarios or dangling links.");
            Expect(inspector.Inspect(textCopy.TextPath).IsValid,"Original-style MAP projection did not reopen.");
            foreach (var item in native.Files) Expect(item.Data.Span.SequenceEqual(snapshots[item.ExportPath]),"Original MAP projection mutated source buffers.");
        });
    }
}

HashSet<string> ReadResourceKeys(string path)
{
    XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
    var keys = XDocument.Load(path)
        .Descendants()
        .Select(element => element.Attribute(xaml + "Key")?.Value)
        .Where(value => value is not null)
        .Select(value => value!)
        .ToArray();
    var duplicates = keys.GroupBy(key => key, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
    Expect(duplicates.Length == 0, $"Duplicate resource keys in {Path.GetFileName(path)}: {string.Join(", ", duplicates)}");
    return keys.ToHashSet(StringComparer.Ordinal);
}

string FindRepoRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "DK2MapEditor.slnx")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate the repository root.");
}

static void WriteUInt32(Span<byte> target, int offset, uint value) =>
    BinaryPrimitives.WriteUInt32LittleEndian(target[offset..], value);

static void WriteUtf16(Span<byte> target, int offset, int byteCount, string value)
{
    var bytes = Encoding.Unicode.GetBytes(value + '\0');
    bytes.AsSpan(0, Math.Min(bytes.Length, byteCount)).CopyTo(target[offset..]);
}

static void Expect(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class TemporaryFixture : IDisposable
{
    public TemporaryFixture()
    {
        DirectoryPath = Path.Combine(Path.GetTempPath(), "DK2MapEditor.Validation", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DirectoryPath);
    }

    public string DirectoryPath { get; }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
