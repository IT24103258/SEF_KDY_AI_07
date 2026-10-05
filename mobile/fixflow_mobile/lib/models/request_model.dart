// Models for Component 1 — Request Intake & Classification
// Matches the C# DTOs: RequestSummaryDto, RequestDetailDto, ClassificationResultDto

class ClassificationResult {
  final String id;
  final String category;
  final String? subcategory;
  final double confidenceScore;
  final bool requiresReview;
  final String? requiredSkill;
  final String? reason;
  final String? detectedAsset;
  final bool isOverride;
  final String? overriddenByUserName;
  final DateTime createdAt;

  const ClassificationResult({
    required this.id,
    required this.category,
    this.subcategory,
    required this.confidenceScore,
    required this.requiresReview,
    this.requiredSkill,
    this.reason,
    this.detectedAsset,
    required this.isOverride,
    this.overriddenByUserName,
    required this.createdAt,
  });

  factory ClassificationResult.fromJson(Map<String, dynamic> json) {
    return ClassificationResult(
      id: json['id'] ?? '',
      category: json['category'] ?? '',
      subcategory: json['subcategory'] as String?,
      confidenceScore: (json['confidenceScore'] as num?)?.toDouble() ?? 0.0,
      requiresReview: json['requiresReview'] as bool? ?? false,
      requiredSkill: json['requiredSkill'] as String?,
      reason: json['reason'] as String?,
      detectedAsset: json['detectedAsset'] as String?,
      isOverride: json['isOverride'] as bool? ?? false,
      overriddenByUserName: json['overriddenByUserName'] as String?,
      createdAt: DateTime.tryParse(json['createdAt'] ?? '') ?? DateTime.now(),
    );
  }
}

/// Photo attached to a request — matches AttachmentDto (Cloudinary SecureUrl).
class RequestAttachment {
  final String id;
  final String secureUrl;
  final String fileName;
  final DateTime createdAt;

  const RequestAttachment({
    required this.id,
    required this.secureUrl,
    required this.fileName,
    required this.createdAt,
  });

  factory RequestAttachment.fromJson(Map<String, dynamic> json) {
    return RequestAttachment(
      id: json['id'] ?? '',
      secureUrl: json['secureUrl'] ?? '',
      fileName: json['fileName'] ?? '',
      createdAt: DateTime.tryParse(json['createdAt'] ?? '') ?? DateTime.now(),
    );
  }
}

class RequestSummary {
  final String id;
  final String requestNumber;
  final String title;
  final String status;
  final String? category;
  final String requesterId;
  final String requesterName;
  final String? locationName;
  final DateTime createdAt;
  final bool hasClassification;

  const RequestSummary({
    required this.id,
    required this.requestNumber,
    required this.title,
    required this.status,
    this.category,
    required this.requesterId,
    required this.requesterName,
    this.locationName,
    required this.createdAt,
    required this.hasClassification,
  });

  factory RequestSummary.fromJson(Map<String, dynamic> json) {
    return RequestSummary(
      id: json['id'] ?? '',
      requestNumber: json['requestNumber'] ?? '',
      title: json['title'] ?? '',
      status: json['status'] ?? '',
      category: json['category'] as String?,
      requesterId: json['requesterId'] ?? '',
      requesterName: json['requesterName'] ?? '',
      locationName: json['locationName'] as String?,
      createdAt: DateTime.tryParse(json['createdAt'] ?? '') ?? DateTime.now(),
      hasClassification: json['hasClassification'] as bool? ?? false,
    );
  }
}

class RequestDetail {
  final String id;
  final String requestNumber;
  final String title;
  final String description;
  final String status;
  final String requesterId;
  final String requesterName;
  final String locationId;
  final String? locationName;
  final String? assetId;
  final String? assetName;
  final String? categoryId;
  final String? categoryName;
  final DateTime createdAt;
  final DateTime? updatedAt;
  final List<ClassificationResult> classifications;
  final List<RequestAttachment> attachments;

  const RequestDetail({
    required this.id,
    required this.requestNumber,
    required this.title,
    required this.description,
    required this.status,
    required this.requesterId,
    required this.requesterName,
    required this.locationId,
    this.locationName,
    this.assetId,
    this.assetName,
    this.categoryId,
    this.categoryName,
    required this.createdAt,
    this.updatedAt,
    required this.classifications,
    this.attachments = const [],
  });

  factory RequestDetail.fromJson(Map<String, dynamic> json) {
    final classJson = json['classifications'] as List<dynamic>? ?? [];
    final attachJson = json['attachments'] as List<dynamic>? ?? [];
    return RequestDetail(
      id: json['id'] ?? '',
      requestNumber: json['requestNumber'] ?? '',
      title: json['title'] ?? '',
      description: json['description'] ?? '',
      status: json['status'] ?? '',
      requesterId: json['requesterId'] ?? '',
      requesterName: json['requesterName'] ?? '',
      locationId: json['locationId'] ?? '',
      locationName: json['locationName'] as String?,
      assetId: json['assetId'] as String?,
      assetName: json['assetName'] as String?,
      categoryId: json['categoryId'] as String?,
      categoryName: json['categoryName'] as String?,
      createdAt: DateTime.tryParse(json['createdAt'] ?? '') ?? DateTime.now(),
      updatedAt: json['updatedAt'] != null
          ? DateTime.tryParse(json['updatedAt'])
          : null,
      classifications: classJson
          .map((e) => ClassificationResult.fromJson(e as Map<String, dynamic>))
          .toList(),
      attachments: attachJson
          .map((e) => RequestAttachment.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}

/// Lightweight DTO for dropdown lists — matches IssueCategoryDto and LocationDto.
class DropdownOption {
  final String id;
  final String name;
  final String building;
  final String floor;
  final String room;

  const DropdownOption({
    required this.id, 
    required this.name,
    this.building = '',
    this.floor = '',
    this.room = '',
  });
}
