import 'package:flutter/material.dart';
import '../core/network/api_client.dart';
import '../models/request_model.dart';
import 'dart:typed_data';

/// State + API layer for Component 1 — Request Intake & Classification.
///
/// Consumed by:
///   - SubmitRequestScreen  (create)
///   - MyRequestsScreen     (list)
///   - RequestDetailScreen  (detail, classify, override)
///
/// Uses the shared ApiClient so the JWT token is always attached.
/// All API calls are GET/POST/PUT via the C# backend — no direct DB access.
class RequestProvider with ChangeNotifier {
  final ApiClient _api;

  RequestProvider({ApiClient? apiClient})
      : _api = apiClient ?? ApiClient();

  // ── Shared state ──────────────────────────────────────────────────────────
  bool _loading = false;
  String? _error;

  bool get isLoading => _loading;
  String? get error => _error;

  void _setLoading(bool v) {
    _loading = v;
    notifyListeners();
  }

  void _setError(String? msg) {
    _error = msg;
    notifyListeners();
  }

  void clearError() => _setError(null);

  // ── Dropdown data (loaded once per screen open) ───────────────────────────
  List<DropdownOption> _locations   = [];
  List<DropdownOption> _categories  = [];
  List<DropdownOption> _assets      = [];

  List<DropdownOption> get locations  => _locations;
  List<DropdownOption> get categories => _categories;
  List<DropdownOption> get assets     => _assets;

  Future<void> loadDropdowns() async {
    try {
      final results = await Future.wait([
        _api.get('/locations'),
        _api.get('/issue-categories'),
        _api.get('/assets'),
      ]);

      final locData  = results[0];
      final catData  = results[1];
      final assetData = results[2];

      _locations = ((locData['data'] as List?) ?? [])
          .map((e) => DropdownOption(
                id: e['id'] ?? '',
                name: e['name'] ?? '',
                building: e['building'] ?? '',
                floor: e['floor'] ?? '',
                room: e['room'] ?? '',
              ))
          .toList();

      _categories = ((catData['data'] as List?) ?? [])
          .map((e) => DropdownOption(id: e['id'] ?? '', name: e['name'] ?? ''))
          .toList();

      _assets = ((assetData['data'] as List?) ?? [])
        .map((e) => DropdownOption(id: e['id'] ?? '', name: e['name'] ?? ''))
        .toList();

      notifyListeners();
    } catch (_) {
      // Non-fatal: screens show empty dropdowns and let the user retry
    }
  }

  // ── My Requests list ──────────────────────────────────────────────────────
  List<RequestSummary> _myRequests  = [];
  int _myTotal      = 0;
  int _myTotalPages = 1;

  List<RequestSummary> get myRequests  => _myRequests;
  int get myTotal       => _myTotal;
  int get myTotalPages  => _myTotalPages;

  Future<void> fetchMyRequests({
    int page = 1,
    int pageSize = 10,
    String? search,
    String? status,
  }) async {
    _setLoading(true);
    _setError(null);

    try {
      final params = <String, String>{
        'page':     page.toString(),
        'pageSize': pageSize.toString(),
        'sortBy':   'CreatedAt',
        'sortDesc': 'true',
        if (search != null && search.isNotEmpty) 'search': search,
        if (status != null && status.isNotEmpty) 'status': status,
      };

      final uri = Uri(
        path: '/requests/me',
        queryParameters: params,
      ).toString();

      final res = await _api.get(uri);
      final data = res['data'] as Map<String, dynamic>? ?? {};

      _myRequests = ((data['items'] as List?) ?? [])
          .map((e) => RequestSummary.fromJson(e as Map<String, dynamic>))
          .toList();
      _myTotal      = (data['totalCount'] as int?) ?? 0;
      _myTotalPages = (data['totalPages'] as int?) ?? 1;
    } catch (e) {
      _setError(e.toString().replaceFirst('AppException: ', ''));
    } finally {
      _setLoading(false);
    }
  }

  // ── Request queue — all requests (Manager/Admin only) ────────────────────
  // Mirrors React's RequestQueuePage: GET /api/requests.
  List<RequestSummary> _allRequests = [];
  int _queueTotal      = 0;
  int _queueTotalPages = 1;

  List<RequestSummary> get allRequests  => _allRequests;
  int get queueTotal      => _queueTotal;
  int get queueTotalPages => _queueTotalPages;

  Future<void> fetchAllRequests({
    int page = 1,
    int pageSize = 20,
    String? search,
    String? status,
    String? category,
    String sortBy = 'CreatedAt',
    bool sortDesc = true,
  }) async {
    _setLoading(true);
    _setError(null);

    try {
      final params = <String, String>{
        'page':     page.toString(),
        'pageSize': pageSize.toString(),
        'sortBy':   sortBy,
        'sortDesc': sortDesc.toString(),
        if (search != null && search.isNotEmpty)   'search':   search,
        if (status != null && status.isNotEmpty)   'status':   status,
        if (category != null && category.isNotEmpty) 'category': category,
      };

      final uri = Uri(
        path: '/requests',
        queryParameters: params,
      ).toString();

      final res = await _api.get(uri);
      final data = res['data'] as Map<String, dynamic>? ?? {};

      _allRequests = ((data['items'] as List?) ?? [])
          .map((e) => RequestSummary.fromJson(e as Map<String, dynamic>))
          .toList();
      _queueTotal      = (data['totalCount'] as int?) ?? 0;
      _queueTotalPages = (data['totalPages'] as int?) ?? 1;
    } catch (e) {
      _setError(e.toString().replaceFirst('AppException: ', ''));
    } finally {
      _setLoading(false);
    }
  }

  // ── Request detail ────────────────────────────────────────────────────────
  RequestDetail? _detail;
  RequestDetail? get detail => _detail;

  Future<void> fetchDetail(String id) async {
    _setLoading(true);
    _setError(null);
    try {
      final res = await _api.get('/requests/$id');
      _detail = RequestDetail.fromJson(
          res['data'] as Map<String, dynamic>);
    } catch (e) {
      _setError(e.toString().replaceFirst('AppException: ', ''));
    } finally {
      _setLoading(false);
    }
  }

  // ── Create request ────────────────────────────────────────────────────────
  Future<String?> createRequest({
    required String title,
    required String description,
    required String locationId,
    String? assetId,
  }) async {
    _setLoading(true);
    _setError(null);
    try {
      final res = await _api.post('/requests', {
        'title':       title,
        'description': description,
        'locationId':  locationId,
        if (assetId != null && assetId.isNotEmpty) 'assetId': assetId,
      });
      final data = res['data'] as Map<String, dynamic>?;
      return data?['id'] as String?;
    } catch (e) {
      _setError(e.toString().replaceFirst('AppException: ', ''));
      return null;
    } finally {
      _setLoading(false);
    }
  }

  Future<bool> uploadAttachment(String requestId, Uint8List bytes, String fileName) async {
    try {
      await _api.uploadFile('/requests/$requestId/attachments', bytes, fileName);
      return true;
    } catch (e) {
      _setError(e.toString().replaceFirst('AppException: ', ''));
      return false;
    }
  }

  // ── Trigger AI classification (Manager/Admin) ─────────────────────────────
  Future<bool> updateRequest(String id, {
    String? title,
    String? description,
    String? locationId,
    String? categoryId,
  }) async {
    _setLoading(true);
    _setError(null);
    try {
      final payload = <String, dynamic>{};
      if (title != null) payload['title'] = title;
      if (description != null) payload['description'] = description;
      if (locationId != null) payload['locationId'] = locationId;
      if (categoryId != null && categoryId.isNotEmpty) {
        payload['categoryId'] = categoryId;
      }

      await _api.put('/requests/$id', payload);
      return true;
    } catch (e) {
      _setError(e.toString().replaceFirst('AppException: ', ''));
      return false;
    } finally {
      _setLoading(false);
    }
  }

  Future<bool> classifyRequest(String id) async {
    _setLoading(true);
    _setError(null);
    try {
      await _api.post('/requests/$id/classify', {});
      await fetchDetail(id); // refresh so UI shows new classification row
      return true;
    } catch (e) {
      _setError(e.toString().replaceFirst('AppException: ', ''));
      return false;
    } finally {
      _setLoading(false);
    }
  }

  // ── Manager override ──────────────────────────────────────────────────────
  Future<bool> overrideClassification({
    required String requestId,
    required String category,
    String? subcategory,
    String? requiredSkill,
    String? reason,
  }) async {
    _setLoading(true);
    _setError(null);
    try {
      await _api.put('/requests/$requestId/classification', {
        'category': category,
        if (subcategory    != null && subcategory.isNotEmpty)    'subcategory':    subcategory,
        if (requiredSkill  != null && requiredSkill.isNotEmpty)  'requiredSkill':  requiredSkill,
        if (reason         != null && reason.isNotEmpty)         'reason':         reason,
      });
      await fetchDetail(requestId);
      return true;
    } catch (e) {
      _setError(e.toString().replaceFirst('AppException: ', ''));
      return false;
    } finally {
      _setLoading(false);
    }
  }

  // ── Delete request ────────────────────────────────────────────────────────
  Future<bool> deleteRequest(String id) async {
    _setLoading(true);
    _setError(null);
    try {
      await _api.delete('/requests/$id');
      return true;
    } catch (e) {
      _setError(e.toString().replaceFirst('AppException: ', ''));
      return false;
    } finally {
      _setLoading(false);
    }
  }
}

