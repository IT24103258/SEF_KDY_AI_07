import 'package:flutter/material.dart';
import '../core/network/api_client.dart';
import '../models/request_model.dart';

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

  List<DropdownOption> get locations  => _locations;
  List<DropdownOption> get categories => _categories;

  Future<void> loadDropdowns() async {
    try {
      final results = await Future.wait([
        _api.get('/locations'),
        _api.get('/issue-categories'),
      ]);

      final locData  = results[0];
      final catData  = results[1];

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
  Future<bool> createRequest({
    required String title,
    required String description,
    required String locationId,
    String? categoryId,
  }) async {
    _setLoading(true);
    _setError(null);
    try {
      await _api.post('/requests', {
        'title':       title,
        'description': description,
        'locationId':  locationId,
        if (categoryId != null && categoryId.isNotEmpty)
          'categoryId': categoryId,
      });
      return true;
    } catch (e) {
      _setError(e.toString().replaceFirst('AppException: ', ''));
      return false;
    } finally {
      _setLoading(false);
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

