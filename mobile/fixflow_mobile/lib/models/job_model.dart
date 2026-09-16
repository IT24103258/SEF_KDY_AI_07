class JobModel {
  final int id;
  final String title;
  final String location;
  final String priority;
  String status;

  JobModel({
    required this.id,
    required this.title,
    required this.location,
    required this.priority,
    required this.status,
  });

  factory JobModel.fromJson(Map<String, dynamic> json) {
    return JobModel(
      id: json['id'] ?? json['requestId'] ?? 0,
      title: json['title'] ?? json['requestTitle'] ?? 'Maintenance Work',
      location: json['location'] ?? json['locationName'] ?? 'Main Campus',
      priority: json['priority'] ?? json['priorityLevel'] ?? 'Medium',
      status: json['status'] ?? 'Assigned',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'title': title,
      'location': location,
      'priority': priority,
      'status': status,
    };
  }
}