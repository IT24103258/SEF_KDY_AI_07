class WorkOrderModel {
  final String id;
  final String workOrderNumber;
  final String title;
  final String description;
  final String requestId;
  final String requestNumber;
  final String requestTitle;
  final String technicianId;
  final String technicianName;
  final String technicianEmployeeId;
  final String technicianSpecialization;
  final String? locationId;
  final String locationName;
  final String building;
  final String room;
  final String priority;
  final String status;
  final DateTime? scheduledStartTime;
  final DateTime? scheduledEndTime;
  final int estimatedDurationMinutes;
  final DateTime? actualStartTime;
  final DateTime? actualEndTime;
  final DateTime? slaDeadline;
  final String? approvedByName;
  final DateTime? approvedAt;
  final String approvalComments;
  final bool conflictDetected;
  final String aiDecisionSummary;
  final List<WorkNoteModel> notes;
  final List<WorkOrderStatusHistoryModel> statusHistories;
  final List<CompletionEvidenceModel> evidence;

  WorkOrderModel({
    required this.id,
    required this.workOrderNumber,
    required this.title,
    required this.description,
    required this.requestId,
    required this.requestNumber,
    this.requestTitle = '',
    required this.technicianId,
    required this.technicianName,
    this.technicianEmployeeId = '',
    this.technicianSpecialization = '',
    this.locationId,
    required this.locationName,
    required this.building,
    required this.room,
    required this.priority,
    required this.status,
    this.scheduledStartTime,
    this.scheduledEndTime,
    required this.estimatedDurationMinutes,
    this.actualStartTime,
    this.actualEndTime,
    this.slaDeadline,
    this.approvedByName,
    this.approvedAt,
    this.approvalComments = '',
    required this.conflictDetected,
    required this.aiDecisionSummary,
    required this.notes,
    this.statusHistories = const [],
    this.evidence = const [],
  });

  String get formattedLocation {
    final parts = <String>[];
    if (room.isNotEmpty) parts.add(room);
    if (building.isNotEmpty) parts.add(building);
    if (locationName.isNotEmpty && !parts.contains(locationName)) parts.add(locationName);
    return parts.isNotEmpty ? parts.join(', ') : (locationName.isNotEmpty ? locationName : 'On Site');
  }

  int get calculatedDurationMinutes {
    if (estimatedDurationMinutes > 0) return estimatedDurationMinutes;
    if (scheduledStartTime != null && scheduledEndTime != null) {
      return scheduledEndTime!.difference(scheduledStartTime!).inMinutes;
    }
    return 60;
  }

  factory WorkOrderModel.fromJson(Map<String, dynamic> json) {
    return WorkOrderModel(
      id: json['id']?.toString() ?? '',
      workOrderNumber: json['workOrderNumber'] ?? '',
      title: json['title'] ?? '',
      description: json['description'] ?? '',
      requestId: json['requestId']?.toString() ?? '',
      requestNumber: json['requestNumber'] ?? '',
      requestTitle: json['requestTitle'] ?? '',
      technicianId: json['technicianId']?.toString() ?? '',
      technicianName: json['technicianName'] ?? '',
      technicianEmployeeId: json['technicianEmployeeId'] ?? '',
      technicianSpecialization: json['technicianSpecialization'] ?? '',
      locationId: json['locationId']?.toString(),
      locationName: json['locationName'] ?? '',
      building: json['building'] ?? '',
      room: json['room'] ?? '',
      priority: json['priority'] ?? 'Medium',
      status: json['status'] ?? 'Scheduled',
      scheduledStartTime: json['scheduledStartTime'] != null
          ? DateTime.tryParse(json['scheduledStartTime'].toString())
          : null,
      scheduledEndTime: json['scheduledEndTime'] != null
          ? DateTime.tryParse(json['scheduledEndTime'].toString())
          : null,
      estimatedDurationMinutes: json['estimatedDurationMinutes'] is int
          ? json['estimatedDurationMinutes']
          : int.tryParse(json['estimatedDurationMinutes']?.toString() ?? '60') ?? 60,
      actualStartTime: json['actualStartTime'] != null
          ? DateTime.tryParse(json['actualStartTime'].toString())
          : null,
      actualEndTime: json['actualEndTime'] != null
          ? DateTime.tryParse(json['actualEndTime'].toString())
          : null,
      slaDeadline: json['slaDeadline'] != null
          ? DateTime.tryParse(json['slaDeadline'].toString())
          : null,
      approvedByName: json['approvedByName'],
      approvedAt: json['approvedAt'] != null
          ? DateTime.tryParse(json['approvedAt'].toString())
          : null,
      approvalComments: json['approvalComments'] ?? '',
      conflictDetected: json['conflictDetected'] ?? false,
      aiDecisionSummary: json['aiDecisionSummary'] ?? '',
      notes: json['notes'] != null
          ? (json['notes'] as List)
              .map((n) => WorkNoteModel.fromJson(n is Map<String, dynamic> ? n : {}))
              .toList()
          : [],
      statusHistories: json['statusHistories'] != null
          ? (json['statusHistories'] as List)
              .map((h) => WorkOrderStatusHistoryModel.fromJson(h is Map<String, dynamic> ? h : {}))
              .toList()
          : [],
      evidence: json['evidence'] != null
          ? (json['evidence'] as List)
              .map((e) => CompletionEvidenceModel.fromJson(e is Map<String, dynamic> ? e : {}))
              .toList()
          : [],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'workOrderNumber': workOrderNumber,
      'title': title,
      'description': description,
      'requestId': requestId,
      'requestNumber': requestNumber,
      'technicianId': technicianId,
      'technicianName': technicianName,
      'locationName': locationName,
      'building': building,
      'room': room,
      'priority': priority,
      'status': status,
      'scheduledStartTime': scheduledStartTime?.toIso8601String(),
      'scheduledEndTime': scheduledEndTime?.toIso8601String(),
      'estimatedDurationMinutes': estimatedDurationMinutes,
      'actualStartTime': actualStartTime?.toIso8601String(),
      'actualEndTime': actualEndTime?.toIso8601String(),
      'slaDeadline': slaDeadline?.toIso8601String(),
      'conflictDetected': conflictDetected,
      'aiDecisionSummary': aiDecisionSummary,
    };
  }
}

class WorkNoteModel {
  final String id;
  final String authorName;
  final String noteText;
  final DateTime timestamp;

  WorkNoteModel({
    required this.id,
    required this.authorName,
    required this.noteText,
    required this.timestamp,
  });

  factory WorkNoteModel.fromJson(Map<String, dynamic> json) {
    return WorkNoteModel(
      id: json['id']?.toString() ?? '',
      authorName: json['authorName'] ?? 'Technician',
      noteText: json['noteText'] ?? '',
      timestamp: json['timestamp'] != null
          ? DateTime.tryParse(json['timestamp'].toString()) ?? DateTime.now()
          : DateTime.now(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'authorName': authorName,
      'noteText': noteText,
      'timestamp': timestamp.toIso8601String(),
    };
  }
}

class WorkOrderStatusHistoryModel {
  final String id;
  final String previousStatus;
  final String newStatus;
  final String? changedByName;
  final String reason;
  final DateTime timestamp;

  WorkOrderStatusHistoryModel({
    required this.id,
    required this.previousStatus,
    required this.newStatus,
    this.changedByName,
    required this.reason,
    required this.timestamp,
  });

  factory WorkOrderStatusHistoryModel.fromJson(Map<String, dynamic> json) {
    return WorkOrderStatusHistoryModel(
      id: json['id']?.toString() ?? '',
      previousStatus: json['previousStatus'] ?? '',
      newStatus: json['newStatus'] ?? '',
      changedByName: json['changedByName'],
      reason: json['reason'] ?? '',
      timestamp: json['timestamp'] != null
          ? DateTime.tryParse(json['timestamp'].toString()) ?? DateTime.now()
          : DateTime.now(),
    );
  }
}

class CompletionEvidenceModel {
  final String id;
  final String workOrderId;
  final String fileKey;
  final String originalFileName;
  final String caption;
  final String? uploadedByName;
  final String signatureDataUrl;
  final String signerName;
  final DateTime uploadedAt;

  CompletionEvidenceModel({
    required this.id,
    required this.workOrderId,
    required this.fileKey,
    required this.originalFileName,
    required this.caption,
    this.uploadedByName,
    required this.signatureDataUrl,
    required this.signerName,
    required this.uploadedAt,
  });

  factory CompletionEvidenceModel.fromJson(Map<String, dynamic> json) {
    return CompletionEvidenceModel(
      id: json['id']?.toString() ?? '',
      workOrderId: json['workOrderId']?.toString() ?? '',
      fileKey: json['fileKey'] ?? '',
      originalFileName: json['originalFileName'] ?? '',
      caption: json['caption'] ?? '',
      uploadedByName: json['uploadedByName'],
      signatureDataUrl: json['signatureDataUrl'] ?? '',
      signerName: json['signerName'] ?? '',
      uploadedAt: json['uploadedAt'] != null
          ? DateTime.tryParse(json['uploadedAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
    );
  }
}
