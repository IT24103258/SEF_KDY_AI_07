/// Read-only mirrors of the ASP.NET Core workflow DTOs returned by
/// `GET /api/agentworkflows`.
///
/// Presentation only. Nothing here decides, recalculates or repairs a
/// Component 2 result — the C# service remains authoritative and these classes
/// simply carry whatever it persisted across to the UI.
library;

class AgentWorkflowModel {
  final String id;
  final String requestId;
  final String requestTitle;
  final String workflowType;
  final String status;
  final DateTime? startedAt;
  final DateTime? completedAt;
  final List<AgentStepModel> steps;

  const AgentWorkflowModel({
    required this.id,
    required this.requestId,
    required this.requestTitle,
    required this.workflowType,
    required this.status,
    this.startedAt,
    this.completedAt,
    this.steps = const [],
  });

  factory AgentWorkflowModel.fromJson(Map<String, dynamic> json) {
    return AgentWorkflowModel(
      id: _asString(json['id']),
      requestId: _asString(json['requestId']),
      requestTitle: _asString(json['requestTitle']),
      workflowType: _asString(json['workflowType']),
      status: _asString(json['status']),
      startedAt: _asDate(json['startedAt']),
      completedAt: _asDate(json['completedAt']),
      steps: _asSteps(json['steps']),
    );
  }

  /// The instant the backend recorded for this workflow, or `null` when it
  /// stored neither timestamp.
  DateTime? get recordedAt => completedAt ?? startedAt;

  String? get recordedAtText => _format(recordedAt);
}

class AgentStepModel {
  final String id;
  final String agentName;
  final String stepName;
  final String status;
  final DateTime? startedAt;
  final DateTime? completedAt;
  final List<AgentToolCallModel> toolCalls;

  const AgentStepModel({
    required this.id,
    required this.agentName,
    required this.stepName,
    required this.status,
    this.startedAt,
    this.completedAt,
    this.toolCalls = const [],
  });

  factory AgentStepModel.fromJson(Map<String, dynamic> json) {
    return AgentStepModel(
      id: _asString(json['id']),
      agentName: _asString(json['agentName']),
      stepName: _asString(json['stepName']),
      status: _asString(json['status']),
      startedAt: _asDate(json['startedAt']),
      completedAt: _asDate(json['completedAt']),
      toolCalls: _asToolCalls(json['toolCalls']),
    );
  }

  /// The timestamp the backend recorded for this step.
  ///
  /// `startedAt` and `completedAt` are written as the same instant by
  /// `PersistWorkflowStateAsync`, so no elapsed duration is derived from them —
  /// showing one would invent a measurement the backend never took.
  DateTime? get recordedAt => completedAt ?? startedAt;

  String? get recordedAtText => _format(recordedAt);
}

class AgentToolCallModel {
  final String toolName;
  final bool success;
  final int executionTimeMs;

  const AgentToolCallModel({
    required this.toolName,
    required this.success,
    required this.executionTimeMs,
  });

  factory AgentToolCallModel.fromJson(Map<String, dynamic> json) {
    final rawMs = json['executionTimeMs'];
    return AgentToolCallModel(
      toolName: _asString(json['toolName']),
      success: json['success'] as bool? ?? true,
      executionTimeMs: rawMs is num ? rawMs.toInt() : 0,
    );
  }
}

String _asString(Object? value) => value?.toString() ?? '';

DateTime? _asDate(Object? value) {
  if (value is String && value.isNotEmpty) return DateTime.tryParse(value);
  return null;
}

List<AgentStepModel> _asSteps(Object? value) {
  if (value is! List) return const [];
  return value
      .whereType<Map<String, dynamic>>()
      .map(AgentStepModel.fromJson)
      .toList(growable: false);
}

List<AgentToolCallModel> _asToolCalls(Object? value) {
  if (value is! List) return const [];
  return value
      .whereType<Map<String, dynamic>>()
      .map(AgentToolCallModel.fromJson)
      .toList(growable: false);
}

String? _format(DateTime? value) {
  if (value == null) return null;
  final local = value.toLocal();
  String two(int n) => n.toString().padLeft(2, '0');
  return '${local.year}-${two(local.month)}-${two(local.day)} '
      '${two(local.hour)}:${two(local.minute)}:${two(local.second)}';
}
