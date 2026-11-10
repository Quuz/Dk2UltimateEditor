using DK2MapEditor.Core.Diagnostics;

namespace DK2MapEditor.Core.Formats;

internal static class NativeOpaqueFile
{
    internal const string Category = "Opaque.NativeFile";
    internal static bool IsUnknown(uint signature) => signature is not (0xDC or 0x64)
        && !NativeCollectionLayout.IsCollection(signature) && !Editing.NativeMapDocument.NativeDefinitionSignature(signature);
    internal static MapDiagnostic Diagnostic(string path) => new(DiagnosticSeverity.Warning, "DK2N049",
        "This native companion has no verified file schema. Raw copying preserves the complete file; review its explicit loss before DKM conversion.", path);
}
