import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../core/network/api_client.dart';
import '../models/priority_assessment_model.dart';
import '../providers/auth_provider.dart';
import '../widgets/priority_badge.dart';

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

  @override
  void initState() {
    super.initState();
    _fetchPriorityData();
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
          final item = PriorityAssessmentModel.fromJson(res['data']);
          setState(() {
            _selectedAssessment = item;
            _assessments = [item];
          });
        }
      } else {
        final res = await _apiClient.get('/priority-assessments?pageSize=20');
        if (res['success'] == true && res['data'] != null) {
          final List items = res['data']['items'] ?? [];
          final list = items.map((i) => PriorityAssessmentModel.fromJson(i)).toList();
          setState(() {
            _assessments = list;
            if (list.isNotEmpty) _selectedAssessment = list.first;
          });
        }
      }
    } catch (e) {
      setState(() {
        _errorMessage = e.toString().replaceFirst('Exception: ', '');
      });
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);
    final userRole = authProvider.user?.role ?? 'Requester';
    final isTechnician = userRole.toLowerCase() == 'technician';

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
      body: _buildBody(isTechnician),
    );
  }

  Widget _buildBody(bool isTechnician) {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24.0),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.error_outline, color: Colors.red, size: 48),
              const SizedBox(height: 12),
              const Text(
                'Unable to load assessment',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
              ),
              const SizedBox(height: 8),
              Text(
                _errorMessage!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: Colors.grey, fontSize: 12),
              ),
              const SizedBox(height: 16),
              ElevatedButton.icon(
                onPressed: _fetchPriorityData,
                icon: const Icon(Icons.refresh, size: 16),
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
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.assignment_turned_in_outlined, color: Colors.grey, size: 48),
              SizedBox(height: 12),
              Text(
                'No Priority Assessments Yet',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
              ),
              SizedBox(height: 8),
              Text(
                'Assessments evaluated by FixFlow AI will be displayed here.',
                textAlign: TextAlign.center,
                style: TextStyle(color: Colors.grey, fontSize: 12),
              ),
            ],
          ),
        ),
      );
    }

    final item = _selectedAssessment ?? _assessments.first;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Selector dropdown if multiple
          if (_assessments.length > 1) ...[
            DropdownButtonFormField<PriorityAssessmentModel>(
              value: item,
              decoration: const InputDecoration(
                labelText: 'Select Maintenance Request',
                border: OutlineInputBorder(),
                contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              ),
              items: _assessments.map((a) {
                return DropdownMenuItem(
                  value: a,
                  child: Text('${a.requestNumber} - ${a.requestTitle}', overflow: TextOverflow.ellipsis),
                );
              }).toList(),
              onChanged: (val) {
                if (val != null) setState(() => _selectedAssessment = val);
              },
            ),
            const SizedBox(height: 16),
          ],

          // Priority Card
          Card(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        item.requestNumber,
                        style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13, color: Colors.grey),
                      ),
                      Row(
                        children: [
                          RiskLevelBadgeWidget(riskLevel: item.riskLevel),
                          const SizedBox(width: 8),
                          PriorityBadgeWidget(priority: item.priority),
                        ],
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  Text(
                    item.requestTitle,
                    style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 8),
                  if (item.assetName != null) ...[
                    Row(
                      children: [
                        const Icon(Icons.build_circle_outlined, size: 16, color: Colors.grey),
                        const SizedBox(width: 6),
                        Text(
                          '${item.assetName} (${item.assetCriticality} Criticality)',
                          style: const TextStyle(fontSize: 12, color: Colors.grey),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                  ],
                  if (item.escalationFlag) ...[
                    Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: Colors.red.withOpacity(0.1),
                        borderRadius: BorderRadius.circular(6),
                      ),
                      child: Row(
                        children: [
                          const Icon(Icons.warning_amber_rounded, color: Colors.red, size: 18),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              'ESCALATED: ${item.escalationReason ?? "Critical priority enforced"}',
                              style: const TextStyle(color: Colors.red, fontSize: 12, fontWeight: FontWeight.bold),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 12),
                  ],
                  const Divider(),
                  const SizedBox(height: 8),

                  // SLA Card
                  SLAResponseCardWidget(
                    window: item.recommendedResponseWindow,
                    responseHours: item.responseTimeHours,
                    resolutionHours: item.resolutionTimeHours,
                  ),

                  // Technician-Specific Details
                  if (isTechnician) ...[
                    const SizedBox(height: 16),
                    const Text(
                      'Technician Dispatch Instructions',
                      style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                    ),
                    const SizedBox(height: 6),
                    Container(
                      padding: const EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        color: Colors.grey.withOpacity(0.08),
                        borderRadius: BorderRadius.circular(6),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('• Urgency: ${item.priority}'),
                          Text('• Operational Impact: ${item.impactLevel}'),
                          Text('• Risk Score: ${item.riskScore} / 100'),
                          const SizedBox(height: 4),
                          Text(
                            'Decision Audit: ${item.explanation}',
                            style: const TextStyle(fontSize: 11, color: Colors.grey),
                          ),
                        ],
                      ),
                    ),
                  ] else ...[
                    // Requester-specific explanation
                    const SizedBox(height: 12),
                    Text(
                      'AI Assessment Summary: ${item.explanation}',
                      style: const TextStyle(fontSize: 12, color: Colors.grey),
                    ),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}