import 'package:flutter/material.dart';
import '../core/network/api_client.dart';
import '../models/priority_assessment_model.dart';
import '../widgets/risk_matrix_widget.dart';

/// Standalone Risk Matrix screen for Component 2 navigation.
/// Loads all assessments and displays them in the 4×4 risk matrix.
class RiskMatrixScreen extends StatefulWidget {
  const RiskMatrixScreen({super.key});

  @override
  State<RiskMatrixScreen> createState() => _RiskMatrixScreenState();
}

class _RiskMatrixScreenState extends State<RiskMatrixScreen> {
  final ApiClient _apiClient = ApiClient();
  bool _loading = true;
  String? _error;
  List<Map<String, dynamic>> _assessmentMaps = [];
  PriorityAssessmentModel? _selectedAssessment;

  @override
  void initState() {
    super.initState();
    _loadAssessments();
  }

  Future<void> _loadAssessments() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final res = await _apiClient.get('/priority-assessments?page=1&pageSize=100');
      if (res['success'] == true && res['data'] != null) {
        final List items = res['data']['items'] ?? [];
        setState(() {
          _assessmentMaps = items.cast<Map<String, dynamic>>();
          if (items.isNotEmpty) {
            _selectedAssessment = PriorityAssessmentModel.fromJson(items.first);
          }
        });
      }
    } catch (e) {
      setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Risk Matrix'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _loadAssessments, tooltip: 'Refresh'),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.error_outline, color: Colors.red, size: 48),
                      const SizedBox(height: 12),
                      Text(_error!, style: const TextStyle(color: Colors.grey)),
                      const SizedBox(height: 12),
                      ElevatedButton(onPressed: _loadAssessments, child: const Text('Retry')),
                    ],
                  ),
                )
              : SingleChildScrollView(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      RiskMatrixWidget(
                        assessments: _assessmentMaps,
                        highlightImpact: _selectedAssessment?.impactLevel,
                        highlightLikelihood: _selectedAssessment?.likelihoodLevel,
                        onCellTap: (impact, likelihood) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text('$impact Impact × $likelihood Likelihood → ${RiskMatrixWidget.getCellRiskLevel(impact, likelihood)}'),
                              duration: const Duration(seconds: 2),
                            ),
                          );
                        },
                      ),
                      const SizedBox(height: 16),
                      // Selected assessment info
                      if (_selectedAssessment != null) ...[
                        Card(
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                          child: Padding(
                            padding: const EdgeInsets.all(14),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text('Highlighted Assessment', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13, color: Colors.grey)),
                                const SizedBox(height: 6),
                                Text(
                                  '${_selectedAssessment!.requestNumber} — ${_selectedAssessment!.requestTitle}',
                                  style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  'Impact: ${_selectedAssessment!.impactLevel} • Likelihood: ${_selectedAssessment!.likelihoodLevel} • Risk: ${_selectedAssessment!.riskLevel}',
                                  style: const TextStyle(fontSize: 12, color: Colors.grey),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
    );
  }
}
