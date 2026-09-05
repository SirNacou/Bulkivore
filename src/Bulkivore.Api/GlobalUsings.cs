global using ErrorOr;
global using FluentValidation;
global using static Bulkivore.Api.Domain.Ingestion.ParsedCell;
using Vogen;


[assembly: VogenDefaults(
    underlyingType: typeof(Guid),
    conversions: Conversions.Default | Conversions.EfCoreValueConverter,
    throws: null,
    customizations: Customizations.AddFactoryMethodForGuids,
    deserializationStrictness: DeserializationStrictness.Default,
    debuggerAttributes: DebuggerAttributeGeneration.Basic,
    toPrimitiveCasting: CastOperator.Explicit,
    fromPrimitiveCasting: CastOperator.Explicit,
    disableStackTraceRecordingInDebug: false,
    parsableForStrings: ParsableForStrings.GenerateMethodsAndInterface,
    parsableForPrimitives: ParsableForPrimitives.HoistMethodsAndInterfaces,
    tryFromGeneration: TryFromGeneration.GenerateBoolAndErrorOrMethods,
    isInitializedMethodGeneration: IsInitializedMethodGeneration.Generate,
    systemTextJsonConverterFactoryGeneration: SystemTextJsonConverterFactoryGeneration.Generate,
    staticAbstractsGeneration: StaticAbstractsGeneration.MostCommon,
    openApiSchemaCustomizations: OpenApiSchemaCustomizations.GenerateOpenApiMappingExtensionMethod,
    explicitlySpecifyTypeInValueObject: true,
    primitiveEqualityGeneration: PrimitiveEqualityGeneration.GenerateOperatorsAndMethods,
    numericsGeneration: NumericsGeneration.Omit,
    stringDefaultComparison: StringComparisonDefault.Omit
)]
