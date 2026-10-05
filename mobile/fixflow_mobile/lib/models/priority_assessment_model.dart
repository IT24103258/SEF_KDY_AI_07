class PriorityAssessmentModel {
  final String id;
  final String requestId;
  final String requestNumber;
  final String requestTitle;
  final String? assetName;
  final String? locationName;
  final String assetCriticality;
  final String impactLevel;
  final String likelihoodLevel;
  final int riskScore;
  final String riskLevel;
  final String priority;
  final String recommendedResponseWindow;
  final int responseTimeHours;
  final int resolutionTimeHours;
  final bool escalationFlag;
  final String? escalationReason;
  final String explanation;
  final String status;
  final bool humanApprovalRequired;
  final bool hazardDetected;

  /// Deterministic factors computed and persisted by the C# PriorityAssessmentService.
  /// Null when the backend did not include them — never synthesized on the client.
  final ContributingFactors? contributingFactors;

  PriorityAssessmentModel({
    required this.id,
    required this.requestId,
    required this.requestNumber,
    required this.requestTitle,
    this.assetName,
    this.locationName,
    required this.assetCriticality,
    required this.impactLevel,
    required this.likelihoodLevel,
    required this.riskScore,
    required this.riskLevel,
    required this.priority,
    required this.recommendedResponseWindow,
    required this.responseTimeHours,
    required this.resolutionTimeHours,
    required this.escalationFlag,
    this.escalationReason,
    required this.explanation,
    required this.status,
    this.humanApprovalRequired = false,
    this.hazardDetected = false,
    this.contributingFactors,
  });

  factory PriorityAssessmentModel.fromJson(Map<String, dynamic> json) {
    final factors = json['contributingFactors'];
    return PriorityAssessmentModel(
      id: json['id'] ?? '',
      requestId: json['requestId'] ?? '',
      requestNumber: json['requestNumber'] ?? '',
      requestTitle: json['requestTitle'] ?? '',
      assetName: json['assetName'],
      locationName: json['locationName'],
      assetCriticality: json['assetCriticality'] ?? 'Medium',
      impactLevel: json['impactLevel'] ?? 'Medium',
      likelihoodLevel: json['likelihoodLevel'] ?? 'Medium',
      riskScore: json['riskScore'] ?? 0,
      riskLevel: json['riskLevel'] ?? 'Medium',
      priority: json['priority'] ?? 'Medium',
      recommendedResponseWindow: json['recommendedResponseWindow'] ?? 'Within 4 hours',
      responseTimeHours: json['responseTimeHours'] ?? 4,
      resolutionTimeHours: json['resolutionTimeHours'] ?? 24,
      escalationFlag: json['escalationFlag'] ?? false,
      escalationReason: json['escalationReason'],
      explanation: json['explanation'] ?? '',
      status: json['status'] ?? 'Active',
      humanApprovalRequired: json['humanApprovalRequired'] ?? false,
      hazardDetected: json['hazardDetected'] ?? false,
      contributingFactors: factors is Map<String, dynamic>
          ? ContributingFactors.fromJson(factors)
          : null,
    );
  }
}

/// Read-only mirror of the backend `ContributingFactorsDto`.
///
/// Every field is nullable on purpose: a factor the backend did not report must
/// render as unavailable rather than being defaulted to a plausible-looking 0,
/// which would misrepresent the authoritative assessment.
class ContributingFactors {
  final String? assetCriticality;
  final int? baseMatrixScore;
  final int? assetCriticalityScore;
  final int? impactScore;
  final int? likelihoodScore;
  final bool? hasSafetyHazard;
  final int? safetyHazardModifier;
  final int? recurrenceModifier;
  final int? locationModifier;
  final int? recentFailureCount;
  final String? operationalDisruption;

  const ContributingFactors({
    this.assetCriticality,
    this.baseMatrixScore,
    this.assetCriticalityScore,
    this.impactScore,
    this.likelihoodScore,
    this.hasSafetyHazard,
    this.safetyHazardModifier,
    this.recurrenceModifier,
    this.locationModifier,
    this.recentFailureCount,
    this.operationalDisruption,
  });

  factory ContributingFactors.fromJson(Map<String, dynamic> json) {
    return ContributingFactors(
      assetCriticality: json['assetCriticality'] as String?,
      baseMatrixScore: _asInt(json['baseMatrixScore']),
      assetCriticalityScore: _asInt(json['assetCriticalityScore']),
      impactScore: _asInt(json['impactScore']),
      likelihoodScore: _asInt(json['likelihoodScore']),
      hasSafetyHazard: json['hasSafetyHazard'] as bool?,
      safetyHazardModifier: _asInt(json['safetyHazardModifier']),
      recurrenceModifier: _asInt(json['recurrenceModifier']),
      locationModifier: _asInt(json['locationModifier']),
      recentFailureCount: _asInt(json['recentFailureCount']),
      operationalDisruption: json['operationalDisruption'] as String?,
    );
  }

  static int? _asInt(Object? value) {
    if (value is int) return value;
    if (value is num) return value.toInt();
    return null;
  }
}
