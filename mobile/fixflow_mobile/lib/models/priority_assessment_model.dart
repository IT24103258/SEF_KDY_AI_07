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
  });

  factory PriorityAssessmentModel.fromJson(Map<String, dynamic> json) {
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
    );
  }
}
