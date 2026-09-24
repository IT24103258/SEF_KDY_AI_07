class WorkOrderModel {
  final String id;
  final String workOrderNumber;
  final String title;
  final String description;
  final String requestId;
  final String requestNumber;
  final String technicianId;
  final String technicianName;
  final String locationName;
  final String building;
  final String room;
  final String priority;
  final String status;
  final DateTime? scheduledStartTime;
  final DateTime? scheduledEndTime;
  final int estimatedDurationMinutes;
  final DateTime? slaDeadline;
  final bool conflictDetected;
  final String aiDecisionSummary;
  final List<WorkNoteModel> notes;

  WorkOrderModel({
    required this.id,
    required this.workOrderNumber,
    required this.title,
    required this.description,
    required this.requestId,
    required this.requestNumber,
    required this.technicianId,
    required this.technicianName,
    required this.locationName,
    required this.building,
    required this.room,
    required this.priority,
    required this.status,
    this.scheduledStartTime,
    this.scheduledEndTime,
    required this.estimatedDurationMinutes,
    this.slaDeadline,
    required this.conflictDetected,
    required this.aiDecisionSummary,
    required this.notes,
  });

  factory WorkOrderModel.fromJson(Map<String, dynamic> json) {
    return WorkOrderModel(
      id: json['id']?.toString() ?? '',
      workOrderNumber: json['workOrderNumber'] ?? '',
      title: json['title'] ?? '',
      description: json['description'] ?? '',
      requestId: json['requestId']?.toString() ?? '',
      requestNumber: json['requestNumber'] ?? '',
      technicianId: json['technicianId']?.toString() ?? '',
      technicianName: json['technicianName'] ?? '',
      locationName: json['locationName'] ?? '',
      building: json['building'] ?? '',
      room: json['room'] ?? '',
      priority: json['priority'] ?? 'Medium',
      status: json['status'] ?? 'Scheduled',
      scheduledStartTime: json['scheduledStartTime'] != null
          ? DateTime.tryParse(json['scheduledStartTime'])
          : null,
      scheduledEndTime: json['scheduledEndTime'] != null
          ? DateTime.tryParse(json['scheduledEndTime'])
          : null,
      estimatedDurationMinutes: json['estimatedDurationMinutes'] ?? 60,
      slaDeadline: json['slaDeadline'] != null
          ? DateTime.tryParse(json['slaDeadline'])
          : null,
      conflictDetected: json['conflictDetected'] ?? false,
      aiDecisionSummary: json['aiDecisionSummary'] ?? '',
      notes: json['notes'] != null
          ? (json['notes'] as List)
              .map((n) => WorkNoteModel.fromJson(n))
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
      'priority': priority,
      'status': status,
      'scheduledStartTime': scheduledStartTime?.toIso8601String(),
      'scheduledEndTime': scheduledEndTime?.toIso8601String(),
      'estimatedDurationMinutes': estimatedDurationMinutes,
      'conflictDetected': conflictDetected,
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
          ? DateTime.tryParse(json['timestamp']) ?? DateTime.now()
          : DateTime.now(),
    );
  }
}
