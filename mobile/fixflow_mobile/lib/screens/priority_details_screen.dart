import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';
import '../core/constants/api_constants.dart';
import '../core/errors/app_exception.dart';
import '../core/network/api_client.dart';
import '../core/storage/secure_storage_service.dart';
import '../models/priority_assessment_model.dart';
import '../providers/auth_provider.dart';
import '../widgets/priority_badge.dart';
import '../widgets/escalation_action_button.dart';

class PriorityDetailsScreen extends StatefulWidget {
  final String? requestId;

  const PriorityDetailsScreen({super.key, this.requestId});

  @override
  State<PriorityDetailsScreen> createState() => _PriorityDetailsScreenState();
}

class _PriorityDetailsScreenState extends State<PriorityDetailsScreen> {
  final ApiClient _apiClient = ApiClient();
  bool _isLoading = true;
  String? _errorMessage;
  List<PriorityAssessmentModel> _assessments = [];
  PriorityAssessmentModel? _selectedAssessment;

  // Search & Pagination
  final TextEditingController _searchController = TextEditingController();
  int _currentPage = 1;
  final int _pageSize = 10;
  int _totalCount = 0;
  bool _isActionLoading = false;

  // Filters (matching React Component 2 filter capabilities)
  String _priorityFilter = 'All';
  String _riskLevelFilter = 'All';
  String _escalationFilter = 'All';
  String _sortBy = 'newest';

  static const List<String> priorityFilterOptions = [
    'All',
    'Low',
    'Medium',
    'High',
    'Critical'
  ];

  static const List<String> riskLevelFilterOptions = [
    'All',
    'Low',
    'Medium',
    'High',
    'Critical'
  ];

  static const List<String> escalationFilterOptions = [
    'All',
    'Escalated',
    'Not Escalated'
  ];

  static const Map<String, String> sortOptions = {
    'newest': 'Newest First',
    'oldest': 'Oldest First',
    'highest_risk': 'Highest Risk',
    'lowest_risk': 'Lowest Risk',
    'highest_priority': 'Highest Priority',
    'lowest_priority': 'Lowest Priority',
  };

  @override
  void initState() {
    super.initState();
    _searchController.addListener(_onSearchChanged);
    _fetchPriorityData();
  }

  void _onSearchChanged() {
    setState(() {});
  }

  @override
  void dispose() {
    _searchController.removeListener(_onSearchChanged);
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _fetchPriorityData() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      if (widget.requestId != null && widget.requestId!.isNotEmpty) {
        final res = await _apiClient.get('/priorities/${widget.requestId}');

        if (res['success'] == true && res['data'] != null) {
          final item =
              PriorityAssessmentModel.fromJson(res['data']);

          setState(() {
            _selectedAssessment = item;
            _assessments = [item];
            _totalCount = 1;
          });
        }
      } else {
        final queryParams = [
          'page=$_currentPage',
          'pageSize=$_pageSize',
          'sortBy=${Uri.encodeComponent(_sortBy)}',
          if (_searchController.text.trim().isNotEmpty)
            'searchTerm=${Uri.encodeComponent(_searchController.text.trim())}',
          if (_priorityFilter != 'All')
            'priority=${Uri.encodeComponent(_priorityFilter)}',
          if (_riskLevelFilter != 'All')
            'riskLevel=${Uri.encodeComponent(_riskLevelFilter)}',
          if (_escalationFilter == 'Escalated')
            'escalatedOnly=true',
          if (_escalationFilter == 'Not Escalated')
            'escalatedOnly=false',
        ].join('&');

        final res =
            await _apiClient.get('/priority-assessments?$queryParams');

        if (res['success'] == true && res['data'] != null) {
          final List items = res['data']['items'] ?? [];

          final list = items
              .map((i) => PriorityAssessmentModel.fromJson(i))
              .toList();

          // IMPORTANT:
          // Every API refresh creates new PriorityAssessmentModel objects.
          // The selected assessment must reference the exact object instance
          // contained in the newly fetched _assessments list. Otherwise,
          // DropdownButtonFormField can throw:
          // "There should be exactly one item with [DropdownButton]'s value".
          setState(() {
            _assessments = list;
            _totalCount =
                res['data']['totalCount'] ?? list.length;

            if (list.isNotEmpty) {
              final selectedId = _selectedAssessment?.id;

              PriorityAssessmentModel? matchingAssessment;

              if (selectedId != null) {
                for (final assessment in list) {
                  if (assessment.id == selectedId) {
                    matchingAssessment = assessment;
                    break;
                  }
                }
              }

              // Use the newly fetched object from _assessments.
              // This guarantees the dropdown value matches one of
              // the DropdownMenuItem objects by identity.
              _selectedAssessment =
                  matchingAssessment ?? list.first;
            } else {
              _selectedAssessment = null;
            }
          });
        }
      }
    } catch (e) {
      setState(() {
        _errorMessage =
            e.toString().replaceFirst('Exception: ', '');
      });
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  Future<void> _escalate(
    PriorityAssessmentModel item,
    String reason,
    bool hazard,
  ) async {
    setState(() => _isActionLoading = true);

    final messenger = ScaffoldMessenger.of(context);
    final authProvider =
        Provider.of<AuthProvider>(context, listen: false);

    final userRole =
        authProvider.user?.role.toLowerCase() ?? '';

    final canManage =
        userRole == 'manager' ||
        userRole == 'administrator';

    if (authProvider.user != null && !canManage) {
      messenger.showSnackBar(
        const SnackBar(
          content: Text(
            'Unauthorized: Administrator or Manager role required to escalate.',
          ),
        ),
      );

      if (mounted) {
        setState(() => _isActionLoading = false);
      }

      return;
    }

    try {
      final res = await _apiClient.post(
        '/requests/${item.requestId}/escalate',
        {
          'reason': reason,
          'immediateHazard': hazard,
        },
      );

      if (!mounted) return;

      if (res['success'] == true) {
        messenger.showSnackBar(
          const SnackBar(
            content: Text(
              'Request escalated to Critical successfully',
            ),
          ),
        );

        await _fetchPriorityData();
      } else {
        messenger.showSnackBar(
          SnackBar(
            content:
                Text(res['message'] ?? 'Escalation failed'),
          ),
        );
      }
    } on AppException catch (e) {
      if (e.statusCode == 401 || e.statusCode == 403) {
        messenger.showSnackBar(
          const SnackBar(
            content: Text(
              'Unauthorized: Administrator or Manager role required to escalate.',
            ),
          ),
        );
      } else {
        messenger.showSnackBar(
          SnackBar(
            content:
                Text('Escalation failed: ${e.message}'),
          ),
        );
      }
    } catch (e) {
      messenger.showSnackBar(
        SnackBar(
          content: Text('Error: $e'),
        ),
      );
    } finally {
      if (mounted) {
        setState(() => _isActionLoading = false);
      }
    }
  }

  Future<void> _deEscalate(
    PriorityAssessmentModel item,
    String reason,
  ) async {
    setState(() => _isActionLoading = true);

    final messenger = ScaffoldMessenger.of(context);
    final authProvider =
        Provider.of<AuthProvider>(context, listen: false);

    final userRole =
        authProvider.user?.role.toLowerCase() ?? '';

    final canManage =
        userRole == 'manager' ||
        userRole == 'administrator';

    if (authProvider.user != null && !canManage) {
      messenger.showSnackBar(
        const SnackBar(
          content: Text(
            'Unauthorized: Administrator or Manager role required to de-escalate.',
          ),
        ),
      );

      if (mounted) {
        setState(() => _isActionLoading = false);
      }

      return;
    }

    try {
      final res = await _apiClient.post(
        '/requests/${item.requestId}/de-escalate',
        {
          'reason': reason,
        },
      );

      if (!mounted) return;

      if (res['success'] == true) {
        messenger.showSnackBar(
          const SnackBar(
            content: Text(
              'Request de-escalated successfully',
            ),
          ),
        );

        await _fetchPriorityData();
      } else {
        messenger.showSnackBar(
          SnackBar(
            content: Text(
              res['message'] ?? 'De-escalation failed',
            ),
          ),
        );
      }
    } on AppException catch (e) {
      if (e.statusCode == 401 || e.statusCode == 403) {
        messenger.showSnackBar(
          const SnackBar(
            content: Text(
              'Unauthorized: Administrator or Manager role required to de-escalate.',
            ),
          ),
        );
      } else {
        messenger.showSnackBar(
          SnackBar(
            content:
                Text('De-escalation failed: ${e.message}'),
          ),
        );
      }
    } catch (e) {
      messenger.showSnackBar(
        SnackBar(
          content: Text('Error: $e'),
        ),
      );
    } finally {
      if (mounted) {
        setState(() => _isActionLoading = false);
      }
    }
  }

  Future<void> _rerunAgentAssessment(
    PriorityAssessmentModel item,
  ) async {
    setState(() => _isActionLoading = true);

    final messenger = ScaffoldMessenger.of(context);

    try {
      final res = await _apiClient.post(
        '/requests/${item.requestId}/priority-agent/assess',
        {},
      );

      if (!mounted) return;

      if (res['success'] == true) {
        messenger.showSnackBar(
          const SnackBar(
            content: Text(
              'Agent assessment re-evaluated and persisted successfully',
            ),
          ),
        );

        await _fetchPriorityData();
      } else {
        messenger.showSnackBar(
          SnackBar(
            content: Text(
              res['message'] ??
                  'Re-run assessment failed',
            ),
          ),
        );
      }
    } catch (e) {
      messenger.showSnackBar(
        SnackBar(
          content: Text('Error: $e'),
        ),
      );
    } finally {
      if (mounted) {
        setState(() => _isActionLoading = false);
      }
    }
  }

  Future<void> _overrideAssessment(
    PriorityAssessmentModel item,
    String priority,
    String riskLevel,
    String reason,
  ) async {
    setState(() => _isActionLoading = true);

    final messenger = ScaffoldMessenger.of(context);

    try {
      final token =
          await SecureStorageService().getToken();

      final client = http.Client();

      final response = await client.put(
        Uri.parse(
          '${ApiConstants.baseUrl}/priority-assessments/${item.id}',
        ),
        headers: {
          'Content-Type': 'application/json',
          if (token != null)
            'Authorization': 'Bearer $token',
        },
        body: jsonEncode({
          'priority': priority,
          'riskLevel': riskLevel,
          'explanation': reason,
        }),
      );

      if (!mounted) return;

      final res =
          jsonDecode(response.body) as Map<String, dynamic>;

      if (response.statusCode >= 200 &&
          response.statusCode < 300 &&
          res['success'] == true) {
        messenger.showSnackBar(
          const SnackBar(
            content: Text(
              'Priority assessment override saved successfully',
            ),
          ),
        );

        await _fetchPriorityData();
      } else {
        messenger.showSnackBar(
          SnackBar(
            content:
                Text(res['message'] ?? 'Override failed'),
          ),
        );
      }
    } catch (e) {
      messenger.showSnackBar(
        SnackBar(
          content: Text('Error: $e'),
        ),
      );
    } finally {
      if (mounted) {
        setState(() => _isActionLoading = false);
      }
    }
  }

  void _showOverrideDialog(
    PriorityAssessmentModel item,
  ) {
    String selectedPriority = item.priority;
    String selectedRiskLevel = item.riskLevel;

    final reasonController = TextEditingController();

    showDialog(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: const Text(
            'Override Assessment',
            style: TextStyle(color: Colors.indigo),
          ),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                'Override priority and risk level for ${item.requestNumber}.',
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                initialValue: selectedPriority,
                decoration: const InputDecoration(
                  labelText: 'Priority Level',
                  border: OutlineInputBorder(),
                ),
                items: [
                  'Critical',
                  'High',
                  'Medium',
                  'Low'
                ]
                    .map(
                      (p) => DropdownMenuItem(
                        value: p,
                        child: Text(p),
                      ),
                    )
                    .toList(),
                onChanged: (val) {
                  if (val != null) {
                    setDialogState(() {
                      selectedPriority = val;
                      selectedRiskLevel = val;
                    });
                  }
                },
              ),
              const SizedBox(height: 12),
              TextField(
                controller: reasonController,
                decoration: const InputDecoration(
                  labelText: 'Override Justification *',
                  border: OutlineInputBorder(),
                ),
                maxLines: 2,
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('Cancel'),
            ),
            ElevatedButton(
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.indigo,
              ),
              onPressed: () {
                if (reasonController.text.trim().isEmpty) {
                  return;
                }

                Navigator.pop(ctx);

                _overrideAssessment(
                  item,
                  selectedPriority,
                  selectedRiskLevel,
                  reasonController.text.trim(),
                );
              },
              child: const Text(
                'Save Override',
                style: TextStyle(color: Colors.white),
              ),
            ),
          ],
        ),
      ),
    );
  }

  /// Builds a single labelled filter row with icon + dropdown.
  Widget _buildFilterDropdown({
    required String label,
    required IconData icon,
    required String value,
    required List<String> options,
    required ValueChanged<String> onChanged,
  }) {
    return Row(
      children: [
        Icon(
          icon,
          size: 16,
          color: Colors.grey[600],
        ),
        const SizedBox(width: 8),
        SizedBox(
          width: 88,
          child: Text(
            label,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
        Expanded(
          child: Container(
            decoration: BoxDecoration(
              border: Border.all(
                color:
                    Colors.grey.withValues(alpha: 0.35),
              ),
              borderRadius: BorderRadius.circular(8),
            ),
            padding:
                const EdgeInsets.symmetric(horizontal: 12),
            child: DropdownButtonHideUnderline(
              child: DropdownButton<String>(
                value: value,
                isExpanded: true,
                isDense: false,
                icon: const Icon(
                  Icons.keyboard_arrow_down,
                  size: 18,
                ),
                style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w500,
                ),
                items: options
                    .map(
                      (opt) => DropdownMenuItem(
                        value: opt,
                        child: Text(
                          opt,
                          style: TextStyle(
                            fontSize: 13,
                            color: opt == 'All'
                                ? Colors.grey[700]
                                : null,
                            fontWeight:
                                opt == value &&
                                        opt != 'All'
                                    ? FontWeight.w700
                                    : FontWeight.w500,
                          ),
                        ),
                      ),
                    )
                    .toList(),
                onChanged: (val) {
                  if (val != null) {
                    onChanged(val);
                  }
                },
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildSortDropdown() {
    return Row(
      children: [
        Icon(
          Icons.sort,
          size: 16,
          color: Colors.grey[600],
        ),
        const SizedBox(width: 8),
        const SizedBox(
          width: 88,
          child: Text(
            'Sort By',
            style: TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
        Expanded(
          child: Container(
            decoration: BoxDecoration(
              border: Border.all(
                color:
                    Colors.grey.withValues(alpha: 0.35),
              ),
              borderRadius: BorderRadius.circular(8),
            ),
            padding:
                const EdgeInsets.symmetric(horizontal: 12),
            child: DropdownButtonHideUnderline(
              child: DropdownButton<String>(
                value: _sortBy,
                isExpanded: true,
                isDense: false,
                icon: const Icon(
                  Icons.keyboard_arrow_down,
                  size: 18,
                ),
                style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w500,
                ),
                items: sortOptions.entries
                    .map(
                      (e) => DropdownMenuItem(
                        value: e.key,
                        child: Text(
                          e.value,
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: e.key == _sortBy
                                ? FontWeight.w700
                                : FontWeight.w500,
                          ),
                        ),
                      ),
                    )
                    .toList(),
                onChanged: (val) {
                  if (val != null) {
                    setState(() {
                      _sortBy = val;
                      _currentPage = 1;
                    });

                    _fetchPriorityData();
                  }
                },
              ),
            ),
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Priority & SLA Tracking'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _fetchPriorityData,
            tooltip: 'Refresh',
          ),
        ],
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(
        child: CircularProgressIndicator(),
      );
    }

    if (_errorMessage != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24.0),
          child: Column(
            mainAxisAlignment:
                MainAxisAlignment.center,
            children: [
              const Icon(
                Icons.error_outline,
                color: Colors.red,
                size: 48,
              ),
              const SizedBox(height: 12),
              const Text(
                'Unable to load assessment',
                style: TextStyle(
                  fontWeight: FontWeight.bold,
                  fontSize: 16,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                _errorMessage!,
                textAlign: TextAlign.center,
                style: const TextStyle(
                  color: Colors.grey,
                  fontSize: 12,
                ),
              ),
              const SizedBox(height: 16),
              ElevatedButton.icon(
                onPressed: _fetchPriorityData,
                icon: const Icon(
                  Icons.refresh,
                  size: 16,
                ),
                label: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    if (_assessments.isEmpty) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(24.0),
          child: Column(
            mainAxisAlignment:
                MainAxisAlignment.center,
            children: [
              Icon(
                Icons.assignment_turned_in_outlined,
                color: Colors.grey,
                size: 48,
              ),
              SizedBox(height: 12),
              Text(
                'No Priority Assessments Yet',
                style: TextStyle(
                  fontWeight: FontWeight.bold,
                  fontSize: 16,
                ),
              ),
              SizedBox(height: 8),
              Text(
                'Assessments evaluated by FixFlow AI will be displayed here.',
                textAlign: TextAlign.center,
                style: TextStyle(
                  color: Colors.grey,
                  fontSize: 12,
                ),
              ),
            ],
          ),
        ),
      );
    }

    final item =
        _selectedAssessment ?? _assessments.first;

    // Role-based UI hiding.
    final authProvider =
        Provider.of<AuthProvider>(
      context,
      listen: false,
    );

    final userRole =
        authProvider.user?.role.toLowerCase() ?? '';

    final canManage =
        userRole == 'manager' ||
        userRole == 'administrator';

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16.0),
      child: Column(
        crossAxisAlignment:
            CrossAxisAlignment.start,
        children: [
          // Search & Filter Section
          if (widget.requestId == null) ...[
            TextField(
              controller: _searchController,
              decoration: InputDecoration(
                hintText:
                    'Search by request # or title...',
                hintStyle: const TextStyle(
                  fontSize: 13,
                  color: Colors.grey,
                ),
                prefixIcon: const Icon(
                  Icons.search,
                  size: 20,
                  color: Colors.grey,
                ),
                suffixIcon: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    if (_searchController.text.isNotEmpty)
                      IconButton(
                        icon: const Icon(
                          Icons.close,
                          size: 18,
                          color: Colors.grey,
                        ),
                        tooltip: 'Clear search',
                        onPressed: () {
                          _searchController.clear();

                          setState(
                            () => _currentPage = 1,
                          );

                          _fetchPriorityData();
                        },
                      ),
                    IconButton(
                      icon: const Icon(
                        Icons.arrow_forward_rounded,
                        size: 20,
                      ),
                      tooltip: 'Search',
                      onPressed: () {
                        setState(
                          () => _currentPage = 1,
                        );

                        _fetchPriorityData();
                      },
                    ),
                  ],
                ),
                border: OutlineInputBorder(
                  borderRadius:
                      BorderRadius.circular(12),
                  borderSide: BorderSide(
                    color: Colors.grey
                        .withValues(alpha: 0.3),
                  ),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius:
                      BorderRadius.circular(12),
                  borderSide: BorderSide(
                    color: Colors.grey
                        .withValues(alpha: 0.3),
                  ),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius:
                      BorderRadius.circular(12),
                  borderSide: BorderSide(
                    color: Theme.of(context)
                        .colorScheme
                        .primary,
                    width: 1.5,
                  ),
                ),
                filled: true,
                fillColor: Theme.of(context)
                    .colorScheme
                    .surfaceContainerHighest
                    .withValues(alpha: 0.3),
                contentPadding:
                    const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 12,
                ),
              ),
              onSubmitted: (_) {
                setState(
                  () => _currentPage = 1,
                );

                _fetchPriorityData();
              },
            ),
            const SizedBox(height: 12),

            Card(
              elevation: 0,
              shape: RoundedRectangleBorder(
                borderRadius:
                    BorderRadius.circular(12),
                side: BorderSide(
                  color: Theme.of(context)
                      .dividerColor
                      .withValues(alpha: 0.35),
                ),
              ),
              color: Theme.of(context)
                  .colorScheme
                  .surfaceContainerHighest
                  .withValues(alpha: 0.25),
              child: Padding(
                padding:
                    const EdgeInsets.fromLTRB(
                  14,
                  12,
                  14,
                  12,
                ),
                child: Column(
                  crossAxisAlignment:
                      CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Icon(
                          Icons.tune,
                          size: 16,
                          color: Theme.of(context)
                              .colorScheme
                              .primary,
                        ),
                        const SizedBox(width: 8),
                        Text(
                          'FILTERS',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight:
                                FontWeight.w700,
                            letterSpacing: 1.2,
                            color: Theme.of(context)
                                .colorScheme
                                .primary,
                          ),
                        ),
                        if (_priorityFilter != 'All' ||
                            _riskLevelFilter != 'All' ||
                            _escalationFilter !=
                                'All') ...[
                          const SizedBox(width: 8),
                          Container(
                            padding:
                                const EdgeInsets
                                    .symmetric(
                              horizontal: 7,
                              vertical: 2,
                            ),
                            decoration:
                                BoxDecoration(
                              color: Theme.of(context)
                                  .colorScheme
                                  .primary
                                  .withValues(
                                    alpha: 0.15,
                                  ),
                              borderRadius:
                                  BorderRadius
                                      .circular(10),
                            ),
                            child: Text(
                              '${(_priorityFilter != 'All' ? 1 : 0) + (_riskLevelFilter != 'All' ? 1 : 0) + (_escalationFilter != 'All' ? 1 : 0)} active',
                              style: TextStyle(
                                fontSize: 10,
                                fontWeight:
                                    FontWeight.bold,
                                color:
                                    Theme.of(context)
                                        .colorScheme
                                        .primary,
                              ),
                            ),
                          ),
                        ],
                        const Spacer(),
                        if (_priorityFilter != 'All' ||
                            _riskLevelFilter != 'All' ||
                            _escalationFilter !=
                                'All')
                          TextButton(
                            style: TextButton.styleFrom(
                              padding:
                                  const EdgeInsets
                                      .symmetric(
                                horizontal: 8,
                                vertical: 2,
                              ),
                              minimumSize: Size.zero,
                              tapTargetSize:
                                  MaterialTapTargetSize
                                      .shrinkWrap,
                            ),
                            onPressed: () {
                              setState(() {
                                _priorityFilter = 'All';
                                _riskLevelFilter = 'All';
                                _escalationFilter =
                                    'All';
                                _currentPage = 1;
                              });

                              _fetchPriorityData();
                            },
                            child: Text(
                              'Clear filters',
                              style: TextStyle(
                                fontSize: 12,
                                color: Theme.of(context)
                                    .colorScheme
                                    .primary,
                                fontWeight:
                                    FontWeight.w600,
                              ),
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: 12),

                    _buildFilterDropdown(
                      label: 'Priority',
                      icon: Icons.flag_outlined,
                      value: _priorityFilter,
                      options:
                          priorityFilterOptions,
                      onChanged: (val) {
                        setState(() {
                          _priorityFilter = val;
                          _currentPage = 1;
                        });

                        _fetchPriorityData();
                      },
                    ),

                    const SizedBox(height: 8),

                    _buildFilterDropdown(
                      label: 'Risk Level',
                      icon: Icons.shield_outlined,
                      value: _riskLevelFilter,
                      options:
                          riskLevelFilterOptions,
                      onChanged: (val) {
                        setState(() {
                          _riskLevelFilter = val;
                          _currentPage = 1;
                        });

                        _fetchPriorityData();
                      },
                    ),

                    const SizedBox(height: 8),

                    _buildFilterDropdown(
                      label: 'Escalation',
                      icon:
                          Icons.warning_amber_outlined,
                      value: _escalationFilter,
                      options:
                          escalationFilterOptions,
                      onChanged: (val) {
                        setState(() {
                          _escalationFilter = val;
                          _currentPage = 1;
                        });

                        _fetchPriorityData();
                      },
                    ),

                    const SizedBox(height: 8),

                    _buildSortDropdown(),
                  ],
                ),
              ),
            ),

            const SizedBox(height: 12),

            // Quick-access tool cards
            Row(
              children: [
                Expanded(
                  child: Material(
                    color: Theme.of(context).cardColor,
                    shape:
                        RoundedRectangleBorder(
                      borderRadius:
                          BorderRadius.circular(12),
                      side: BorderSide(
                        color: Colors.indigo
                            .withValues(alpha: 0.35),
                      ),
                    ),
                    elevation: 0.5,
                    child: InkWell(
                      borderRadius:
                          BorderRadius.circular(12),
                      onTap: () =>
                          Navigator.pushNamed(
                        context,
                        '/risk-matrix',
                      ),
                      child: Padding(
                        padding:
                            const EdgeInsets.symmetric(
                          horizontal: 14,
                          vertical: 12,
                        ),
                        child: Column(
                          crossAxisAlignment:
                              CrossAxisAlignment.start,
                          children: [
                            Container(
                              padding:
                                  const EdgeInsets.all(7),
                              decoration:
                                  BoxDecoration(
                                color: Colors.indigo
                                    .withValues(
                                        alpha: 0.12),
                                borderRadius:
                                    BorderRadius.circular(
                                        8),
                              ),
                              child: const Icon(
                                Icons
                                    .grid_4x4_rounded,
                                size: 20,
                                color: Colors.indigo,
                              ),
                            ),
                            const SizedBox(height: 8),
                            const Text(
                              'Risk Matrix',
                              style: TextStyle(
                                fontWeight:
                                    FontWeight.bold,
                                fontSize: 14,
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              'View 4×4 risk matrix',
                              style: TextStyle(
                                fontSize: 11,
                                color: Theme.of(context)
                                        .textTheme
                                        .bodySmall
                                        ?.color ??
                                    Colors.grey,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Material(
                    color: Theme.of(context).cardColor,
                    shape:
                        RoundedRectangleBorder(
                      borderRadius:
                          BorderRadius.circular(12),
                      side: BorderSide(
                        color: Colors.teal
                            .withValues(alpha: 0.35),
                      ),
                    ),
                    elevation: 0.5,
                    child: InkWell(
                      borderRadius:
                          BorderRadius.circular(12),
                      onTap: () {
                        final selected =
                            _selectedAssessment ??
                                (_assessments.isNotEmpty
                                    ? _assessments
                                        .first
                                    : null);

                        if (selected != null) {
                          Navigator.pushNamed(
                            context,
                            '/risk-simulator',
                            arguments: {
                              'requestId':
                                  selected.requestId,
                              'requestNumber':
                                  selected.requestNumber,
                              'assetCriticality':
                                  selected
                                      .assetCriticality,
                              'impactLevel':
                                  selected.impactLevel,
                              'likelihoodLevel':
                                  selected
                                      .likelihoodLevel,
                            },
                          );
                        } else {
                          ScaffoldMessenger.of(
                            context,
                          ).showSnackBar(
                            const SnackBar(
                              content: Text(
                                'No assessment selected for simulator',
                              ),
                            ),
                          );
                        }
                      },
                      child: Padding(
                        padding:
                            const EdgeInsets.symmetric(
                          horizontal: 14,
                          vertical: 12,
                        ),
                        child: Column(
                          crossAxisAlignment:
                              CrossAxisAlignment.start,
                          children: [
                            Container(
                              padding:
                                  const EdgeInsets.all(7),
                              decoration:
                                  BoxDecoration(
                                color: Colors.teal
                                    .withValues(
                                        alpha: 0.12),
                                borderRadius:
                                    BorderRadius.circular(
                                        8),
                              ),
                              child: const Icon(
                                Icons.science_rounded,
                                size: 20,
                                color: Colors.teal,
                              ),
                            ),
                            const SizedBox(height: 8),
                            const Text(
                              'Risk Simulator',
                              style: TextStyle(
                                fontWeight:
                                    FontWeight.bold,
                                fontSize: 14,
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              'Simulate outcomes',
                              style: TextStyle(
                                fontSize: 11,
                                color: Theme.of(context)
                                        .textTheme
                                        .bodySmall
                                        ?.color ??
                                    Colors.grey,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ],
            ),

            const SizedBox(height: 14),
          ],

          // Selector dropdown if multiple
          if (_assessments.length > 1) ...[
            DropdownButtonFormField<
                PriorityAssessmentModel>(
              initialValue: item,
              decoration:
                  const InputDecoration(
                labelText:
                    'Select Maintenance Request',
                border: OutlineInputBorder(),
                contentPadding:
                    EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 8,
                ),
              ),
              items: _assessments.map((a) {
                return DropdownMenuItem(
                  value: a,
                  child: Text(
                    '${a.requestNumber} - ${a.requestTitle}',
                    overflow:
                        TextOverflow.ellipsis,
                  ),
                );
              }).toList(),
              onChanged: (val) {
                if (val != null) {
                  setState(
                    () => _selectedAssessment = val,
                  );
                }
              },
            ),
            const SizedBox(height: 16),
          ],

          // Priority Card
          Card(
            shape:
                RoundedRectangleBorder(
              borderRadius:
                  BorderRadius.circular(14),
            ),
            elevation: 1,
            child: Padding(
              padding:
                  const EdgeInsets.all(16.0),
              child: Column(
                crossAxisAlignment:
                    CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment:
                        MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        item.requestNumber,
                        style:
                            const TextStyle(
                          fontWeight:
                              FontWeight.bold,
                          fontSize: 13,
                          color: Colors.grey,
                        ),
                      ),
                      Container(
                        padding:
                            const EdgeInsets.symmetric(
                          horizontal: 8,
                          vertical: 3,
                        ),
                        decoration:
                            BoxDecoration(
                          color: item.escalationFlag
                              ? Colors.red.withValues(
                                  alpha: 0.12)
                              : Colors.green.withValues(
                                  alpha: 0.1),
                          borderRadius:
                              BorderRadius.circular(6),
                          border: Border.all(
                            color:
                                item.escalationFlag
                                    ? Colors.red
                                        .withValues(
                                            alpha: 0.4)
                                    : Colors.green
                                        .withValues(
                                            alpha: 0.4),
                          ),
                        ),
                        child: Text(
                          item.escalationFlag
                              ? 'ESCALATED'
                              : 'NOT ESCALATED',
                          style: TextStyle(
                            fontSize: 10,
                            fontWeight:
                                FontWeight.bold,
                            color: item.escalationFlag
                                ? Colors.red.shade700
                                : Colors.green
                                    .shade800,
                            letterSpacing: 0.5,
                          ),
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: 10),

                  Text(
                    item.requestTitle,
                    style:
                        const TextStyle(
                      fontSize: 18,
                      fontWeight:
                          FontWeight.bold,
                    ),
                  ),

                  const SizedBox(height: 6),

                  if (item.assetName != null) ...[
                    Row(
                      children: [
                        const Icon(
                          Icons
                              .build_circle_outlined,
                          size: 15,
                          color: Colors.grey,
                        ),
                        const SizedBox(width: 6),
                        Expanded(
                          child: Text(
                            '${item.assetName} • ${item.assetCriticality} Criticality',
                            style:
                                const TextStyle(
                              fontSize: 12,
                              color: Colors.grey,
                            ),
                            overflow:
                                TextOverflow.ellipsis,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 10),
                  ],

                  Row(
                    children: [
                      RiskLevelBadgeWidget(
                        riskLevel:
                            item.riskLevel,
                      ),
                      const SizedBox(width: 8),
                      PriorityBadgeWidget(
                        priority:
                            item.priority,
                      ),
                      const SizedBox(width: 10),
                      Container(
                        padding:
                            const EdgeInsets.symmetric(
                          horizontal: 8,
                          vertical: 4,
                        ),
                        decoration:
                            BoxDecoration(
                          color: Colors.grey
                              .withValues(
                                  alpha: 0.1),
                          borderRadius:
                              BorderRadius.circular(6),
                        ),
                        child: Text(
                          'Score: ${item.riskScore}/100',
                          style:
                              const TextStyle(
                            fontSize: 11,
                            fontWeight:
                                FontWeight.w600,
                          ),
                        ),
                      ),
                    ],
                  ),

                  const SizedBox(height: 12),

                  // Human Approval Required
                  if (item.humanApprovalRequired) ...[
                    Container(
                      padding:
                          const EdgeInsets.all(9),
                      decoration:
                          BoxDecoration(
                        color: Colors.amber
                            .withValues(
                                alpha: 0.15),
                        borderRadius:
                            BorderRadius.circular(8),
                        border: Border.all(
                          color:
                              Colors.amber.shade400,
                        ),
                      ),
                      child: const Row(
                        children: [
                          Icon(
                            Icons
                                .person_pin_circle_outlined,
                            color: Colors.amber,
                            size: 18,
                          ),
                          SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              'FLAGGED FOR DOWNSTREAM HUMAN REVIEW: Component 2 only flags this assessment; Component 4 owns the approval decision.',
                              style: TextStyle(
                                color: Colors.brown,
                                fontSize: 12,
                                fontWeight:
                                    FontWeight.bold,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 8),
                  ],

                  // Failed state
                  if (item.status
                      .toUpperCase() ==
                      'FAILED') ...[
                    Container(
                      padding:
                          const EdgeInsets.all(9),
                      decoration:
                          BoxDecoration(
                        color: Colors.red
                            .withValues(
                                alpha: 0.12),
                        borderRadius:
                            BorderRadius.circular(8),
                        border: Border.all(
                          color:
                              Colors.red.shade400,
                        ),
                      ),
                      child: const Row(
                        children: [
                          Icon(
                            Icons.error_outline,
                            color: Colors.red,
                            size: 18,
                          ),
                          SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              'FAILED — Manual Review Required. This is not a normal assessment.',
                              style: TextStyle(
                                color: Colors.red,
                                fontSize: 12,
                                fontWeight:
                                    FontWeight.bold,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 8),
                  ],

                  // Escalation note
                  if (item.escalationFlag &&
                      item.escalationReason !=
                          null) ...[
                    Container(
                      padding:
                          const EdgeInsets.symmetric(
                        horizontal: 10,
                        vertical: 8,
                      ),
                      decoration:
                          BoxDecoration(
                        color: Colors.red
                            .withValues(
                                alpha: 0.08),
                        borderRadius:
                            BorderRadius.circular(8),
                        border: Border.all(
                          color: Colors.red
                              .withValues(
                                  alpha: 0.25),
                        ),
                      ),
                      child: Row(
                        children: [
                          const Icon(
                            Icons.info_outline,
                            color: Colors.red,
                            size: 16,
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              'Escalation Note: ${item.escalationReason}',
                              style:
                                  const TextStyle(
                                color: Colors.red,
                                fontSize: 12,
                                fontWeight:
                                    FontWeight.w600,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 8),
                  ],

                  const Divider(),
                  const SizedBox(height: 8),

                  SLAResponseCardWidget(
                    window:
                        item.recommendedResponseWindow,
                    responseHours:
                        item.responseTimeHours,
                    resolutionHours:
                        item.resolutionTimeHours,
                  ),

                  const SizedBox(height: 14),

                  const Text(
                    'Technician Dispatch Instructions',
                    style:
                        TextStyle(
                      fontWeight:
                          FontWeight.bold,
                      fontSize: 13,
                    ),
                  ),

                  const SizedBox(height: 6),

                  Container(
                    padding:
                        const EdgeInsets.all(10),
                    decoration:
                        BoxDecoration(
                      color: Colors.grey
                          .withValues(
                              alpha: 0.08),
                      borderRadius:
                          BorderRadius.circular(6),
                    ),
                    child: Column(
                      crossAxisAlignment:
                          CrossAxisAlignment.start,
                      children: [
                        Text(
                          '• Urgency: ${item.priority}',
                        ),
                        Text(
                          '• Operational Impact: ${item.impactLevel}',
                        ),
                        Text(
                          '• Risk Score: ${item.riskScore} / 100',
                        ),
                        const SizedBox(height: 4),
                        Text(
                          'Decision Audit: ${item.explanation}',
                          style:
                              const TextStyle(
                            fontSize: 11,
                            color: Colors.grey,
                          ),
                        ),
                      ],
                    ),
                  ),

                  // Management Actions
                  if (canManage) ...[
                    const SizedBox(height: 14),
                    const Divider(),
                    const SizedBox(height: 8),

                    Row(
                      children: [
                        Icon(
                          Icons
                              .admin_panel_settings_outlined,
                          size: 16,
                          color: Colors.grey[600],
                        ),
                        const SizedBox(width: 6),
                        Text(
                          'Management Actions',
                          style: TextStyle(
                            fontWeight:
                                FontWeight.bold,
                            fontSize: 12,
                            color:
                                Colors.grey[600],
                            letterSpacing: 0.5,
                          ),
                        ),
                      ],
                    ),

                    const SizedBox(height: 10),

                    Wrap(
                      spacing: 8,
                      runSpacing: 8,
                      children: [
                        EscalationActionButton(
                          isEscalated:
                              item.escalationFlag,
                          requestNumber:
                              item.requestNumber,
                          isLoading:
                              _isActionLoading,
                          onEscalate:
                              (reason, hazard) =>
                                  _escalate(
                            item,
                            reason,
                            hazard,
                          ),
                          onDeEscalate:
                              (reason) =>
                                  _deEscalate(
                            item,
                            reason,
                          ),
                        ),

                        ElevatedButton.icon(
                          style:
                              ElevatedButton.styleFrom(
                            backgroundColor:
                                Colors.blue.shade700,
                            foregroundColor:
                                Colors.white,
                            padding:
                                const EdgeInsets
                                    .symmetric(
                              horizontal: 14,
                              vertical: 10,
                            ),
                            shape:
                                RoundedRectangleBorder(
                              borderRadius:
                                  BorderRadius
                                      .circular(8),
                            ),
                          ),
                          icon: _isActionLoading
                              ? const SizedBox(
                                  width: 14,
                                  height: 14,
                                  child:
                                      CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color:
                                        Colors.white,
                                  ),
                                )
                              : const Icon(
                                  Icons.refresh,
                                  size: 16,
                                ),
                          label: const Text(
                            'Re-run Assessment',
                            style:
                                TextStyle(
                              fontWeight:
                                  FontWeight.bold,
                              fontSize: 13,
                            ),
                          ),
                          onPressed:
                              _isActionLoading
                                  ? null
                                  : () =>
                                      _rerunAgentAssessment(
                                        item,
                                      ),
                        ),

                        OutlinedButton.icon(
                          style:
                              OutlinedButton.styleFrom(
                            foregroundColor:
                                Colors.indigo
                                    .shade700,
                            padding:
                                const EdgeInsets
                                    .symmetric(
                              horizontal: 14,
                              vertical: 10,
                            ),
                            shape:
                                RoundedRectangleBorder(
                              borderRadius:
                                  BorderRadius
                                      .circular(8),
                            ),
                          ),
                          icon: const Icon(
                            Icons.tune,
                            size: 16,
                          ),
                          label: const Text(
                            'Override',
                            style:
                                TextStyle(
                              fontWeight:
                                  FontWeight.bold,
                              fontSize: 13,
                            ),
                          ),
                          onPressed:
                              _isActionLoading
                                  ? null
                                  : () =>
                                      _showOverrideDialog(
                                        item,
                                      ),
                        ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
          ),

          // Pagination
          if (widget.requestId == null &&
              _totalCount > _pageSize) ...[
            const SizedBox(height: 16),

            Row(
              mainAxisAlignment:
                  MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Page $_currentPage of ${((_totalCount - 1) ~/ _pageSize) + 1} ($_totalCount items)',
                  style:
                      const TextStyle(
                    fontSize: 12,
                    color: Colors.grey,
                  ),
                ),

                Row(
                  children: [
                    IconButton(
                      icon: const Icon(
                        Icons.chevron_left,
                      ),
                      onPressed:
                          _currentPage > 1
                              ? () {
                                  setState(
                                    () =>
                                        _currentPage--,
                                  );
                                  _fetchPriorityData();
                                }
                              : null,
                    ),

                    IconButton(
                      icon: const Icon(
                        Icons.chevron_right,
                      ),
                      onPressed:
                          (_currentPage *
                                      _pageSize) <
                                  _totalCount
                              ? () {
                                  setState(
                                    () =>
                                        _currentPage++,
                                  );
                                  _fetchPriorityData();
                                }
                              : null,
                    ),
                  ],
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}