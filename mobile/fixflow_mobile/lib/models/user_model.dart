class UserModel {
  final String id;
  final String email;
  final String firstName;
  final String lastName;
  final String role;
  final String phoneNumber;
  final String employeeId;
  final List<String> skills;
  final String specialization;

  UserModel({
    required this.id,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.role,
    this.phoneNumber = '',
    this.employeeId = '',
    this.skills = const [],
    this.specialization = '',
  });

  String get fullName => '$firstName $lastName'.trim();

  bool get isTechnician =>
      role.toLowerCase() == 'technician' || role.toLowerCase() == 'administrator';

  factory UserModel.fromJson(Map<String, dynamic> json) {
    List<String> parsedSkills = [];
    if (json['skills'] != null) {
      if (json['skills'] is List) {
        parsedSkills = (json['skills'] as List).map((s) => s.toString()).toList();
      } else if (json['skills'] is String) {
        parsedSkills = (json['skills'] as String)
            .split(',')
            .map((s) => s.trim())
            .where((s) => s.isNotEmpty)
            .toList();
      }
    }

    return UserModel(
      id: json['id']?.toString() ?? '',
      email: json['email'] ?? '',
      firstName: json['firstName'] ?? '',
      lastName: json['lastName'] ?? '',
      role: json['role'] ?? 'Requester',
      phoneNumber: json['phoneNumber'] ?? '',
      employeeId: json['employeeId'] ?? json['employeeCode'] ?? 'TECH-001',
      skills: parsedSkills.isNotEmpty
          ? parsedSkills
          : ['HVAC Repair', 'Electrical Diagnostics', 'Plumbing'],
      specialization: json['specialization'] ?? 'Senior Maintenance Specialist',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'email': email,
      'firstName': firstName,
      'lastName': lastName,
      'role': role,
      'phoneNumber': phoneNumber,
      'employeeId': employeeId,
      'skills': skills,
      'specialization': specialization,
    };
  }
}
