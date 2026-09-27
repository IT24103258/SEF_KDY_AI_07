import 'package:flutter/material.dart';
import '../core/network/api_client.dart';
import '../widgets/priority_badge.dart';

/// Risk Scenario Simulator Screen for Component 2.
/// Replicates React RiskSimulator.jsx functionality.
/// Read-only sandbox — does NOT persist/mutate any assessment.
class RiskSimulatorScreen extends StatefulWidget {
  final String requestId;
  final String requestNumber;
  final String? initialCriticality;
  final String? initialImpact;
  final String? initialLikelihood;

  const RiskSimulatorScreen({
    super.key,
    required this.requestId,
    required this.requestNumber,
    this.initialCriticality,
    this.initialImpact,
    this.initialLikelihood,
  });

  @override
  State<RiskSimulatorScreen> createState() => _RiskSimulatorScreenState();
}

class _RiskSimulatorScreenState extends State<RiskSimulatorScreen> {
  final ApiClient _apiClient = ApiClient();

  // Simulator inputs
  late String _criticality;
  late String _impact;
  late String _likelihood;
  int _failureCount = 0;
  bool _safetyHazard = false;
  bool _highDensity = false;

  // State
  bool _loading = false;
  String? _error;
  Map<String, dynamic>? _result;

  static const List<String> _levels = ['Low', 'Medium', 'High', 'Critical'];

  @override
  void initState() {
    super.initState();
    _criticality = widget.initialCriticality ?? 'Medium';
    _impact = widget.initialImpact ?? 'Medium';
    _likelihood = widget.initialLikelihood ?? 'Medium';
  }

  Future<void> _runSimulation() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final params = [
        'assetCriticality=${Uri.encodeComponent(_criticality)}',
        'impactLevel=${Uri.encodeComponent(_impact)}',
        'likelihoodLevel=${Uri.encodeComponent(_likelihood)}',
        'hasSafetyHazard=$_safetyHazard',
        'recentFailureCount=$_failureCount',
        'isHighDensityLocation=$_highDensity',
      ].join('&');

      final res = await _apiClient.get('/requests/${widget.requestId}/risk-simulation?$params');

      if (res['success'] == true && res['data'] != null) {
        setState(() => _result = res['data'] as Map<String, dynamic>);
      } else {
        setState(() => _error = res['message']?.toString() ?? 'Simulation could not be calculated.');
      }
    } catch (e) {
      setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      setState(() => _loading = false);
    }
  }

  void _resetToBaseline() {
    setState(() {
      _criticality = widget.initialCriticality ?? 'Medium';
      _impact = widget.initialImpact ?? 'Medium';
      _likelihood = widget.initialLikelihood ?? 'Medium';
      _failureCount = 0;
      _safetyHazard = false;
      _highDensity = false;
      _result = null;
      _error = null;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Risk Scenario Simulator'),
        actions: [
          TextButton.icon(
            onPressed: _resetToBaseline,
            icon: const Icon(Icons.restart_alt, size: 18, color: Colors.white70),
            label: const Text('Reset', style: TextStyle(color: Colors.white70)),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header badge
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: Colors.blue.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(4),
                    border: Border.all(color: Colors.blue.withValues(alpha: 0.3)),
                  ),
                  child: const Text('SANDBOX / READ-ONLY', style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Colors.blue)),
                ),
                const SizedBox(width: 10),
                Text('Target: ${widget.requestNumber}', style: const TextStyle(fontSize: 13, color: Colors.grey)),
              ],
            ),
            const SizedBox(height: 16),

            // Error banner
            if (_error != null) ...[
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: Colors.red.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(6),
                  border: Border.all(color: Colors.red.withValues(alpha: 0.3)),
                ),
                child: Text(_error!, style: const TextStyle(color: Colors.red, fontSize: 13)),
              ),
              const SizedBox(height: 12),
            ],

            // Input Controls Card
            Card(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
              child: Padding(
                padding: const EdgeInsets.all(14),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Simulation Variables', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
                    const SizedBox(height: 12),

                    // Dropdowns Row 1
                    Row(
                      children: [
                        Expanded(child: _buildDropdown('Asset Criticality', _criticality, (v) => setState(() => _criticality = v))),
                        const SizedBox(width: 8),
                        Expanded(child: _buildDropdown('Impact Level', _impact, (v) => setState(() => _impact = v))),
                      ],
                    ),
                    const SizedBox(height: 10),

                    // Dropdowns Row 2
                    Row(
                      children: [
                        Expanded(child: _buildDropdown('Likelihood', _likelihood, (v) => setState(() => _likelihood = v))),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text('Recent Failures', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: Colors.grey)),
                              const SizedBox(height: 4),
                              Row(
                                children: [
                                  IconButton(
                                    icon: const Icon(Icons.remove_circle_outline, size: 20),
                                    onPressed: _failureCount > 0 ? () => setState(() => _failureCount--) : null,
                                    padding: EdgeInsets.zero,
                                    constraints: const BoxConstraints(),
                                  ),
                                  Expanded(
                                    child: Center(
                                      child: Text('$_failureCount', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                                    ),
                                  ),
                                  IconButton(
                                    icon: const Icon(Icons.add_circle_outline, size: 20),
                                    onPressed: _failureCount < 10 ? () => setState(() => _failureCount++) : null,
                                    padding: EdgeInsets.zero,
                                    constraints: const BoxConstraints(),
                                  ),
                                ],
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 10),

                    // Toggle switches
                    SwitchListTile(
                      title: Text(
                        'Safety Hazard Present',
                        style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: _safetyHazard ? Colors.red : null),
                      ),
                      subtitle: const Text('Triggers deterministic override', style: TextStyle(fontSize: 11)),
                      value: _safetyHazard,
                      onChanged: (v) => setState(() => _safetyHazard = v),
                      contentPadding: EdgeInsets.zero,
                      dense: true,
                    ),
                    SwitchListTile(
                      title: const Text('High-Density / Public Area', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600)),
                      value: _highDensity,
                      onChanged: (v) => setState(() => _highDensity = v),
                      contentPadding: EdgeInsets.zero,
                      dense: true,
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 12),

            // Run Simulation Button
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                onPressed: _loading ? null : _runSimulation,
                icon: _loading
                    ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : const Icon(Icons.play_arrow, color: Colors.white),
                label: Text(
                  _loading ? 'Simulating...' : 'Run Simulation',
                  style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
                ),
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.blue,
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                ),
              ),
            ),
            const SizedBox(height: 16),

            // Simulation Result
            if (_result != null) _buildResultCard(),
          ],
        ),
      ),
    );
  }

  Widget _buildDropdown(String label, String value, ValueChanged<String> onChanged) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: Colors.grey)),
        const SizedBox(height: 4),
        DropdownButtonFormField<String>(
          initialValue: value,
          decoration: const InputDecoration(
            border: OutlineInputBorder(),
            contentPadding: EdgeInsets.symmetric(horizontal: 8, vertical: 6),
            isDense: true,
          ),
          items: _levels.map((l) => DropdownMenuItem(value: l, child: Text(l, style: const TextStyle(fontSize: 13)))).toList(),
          onChanged: (v) {
            if (v != null) onChanged(v);
          },
        ),
      ],
    );
  }

  Widget _buildResultCard() {
    final baseline = _result!['baselineAssessment'] as Map<String, dynamic>?;
    final simulated = _result!['simulatedAssessment'] as Map<String, dynamic>?;
    final delta = _result!['delta'] as Map<String, dynamic>?;

    final baseScore = baseline?['riskScore'] ?? 0;
    final simScore = simulated?['riskScore'] ?? 0;
    final scoreDelta = delta?['scoreDelta'] ?? (simScore - baseScore);
    final summary = delta?['summary']?.toString() ?? '';

    return Card(
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(10),
        side: const BorderSide(color: Colors.blue, width: 1),
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text('Simulation Outcome', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: Colors.blue)),
                if (summary.isNotEmpty)
                  Flexible(child: Text(summary, style: const TextStyle(fontSize: 11, color: Colors.grey), textAlign: TextAlign.right)),
              ],
            ),
            const SizedBox(height: 12),

            // Baseline and Simulated side-by-side
            Row(
              children: [
                // Baseline
                Expanded(
                  child: Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: Colors.grey.withValues(alpha: 0.06),
                      borderRadius: BorderRadius.circular(6),
                      border: Border.all(color: Colors.grey.withValues(alpha: 0.2)),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('CURRENT BASELINE', style: TextStyle(fontSize: 9, fontWeight: FontWeight.bold, color: Colors.grey, letterSpacing: 0.5)),
                        const SizedBox(height: 6),
                        Row(
                          children: [
                            Text('$baseScore', style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w800)),
                            const Text(' /100', style: TextStyle(fontSize: 10, color: Colors.grey)),
                          ],
                        ),
                        const SizedBox(height: 4),
                        RiskLevelBadgeWidget(riskLevel: baseline?['riskLevel'] ?? 'Medium'),
                        const SizedBox(height: 4),
                        PriorityBadgeWidget(priority: baseline?['priority'] ?? 'Medium'),
                      ],
                    ),
                  ),
                ),
                // Arrow
                const Padding(
                  padding: EdgeInsets.symmetric(horizontal: 6),
                  child: Icon(Icons.arrow_forward, color: Colors.blue, size: 20),
                ),
                // Simulated
                Expanded(
                  child: Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: Colors.blue.withValues(alpha: 0.08),
                      borderRadius: BorderRadius.circular(6),
                      border: Border.all(color: Colors.blue.withValues(alpha: 0.4)),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('SIMULATED', style: TextStyle(fontSize: 9, fontWeight: FontWeight.bold, color: Colors.blue, letterSpacing: 0.5)),
                        const SizedBox(height: 6),
                        Row(
                          children: [
                            Text('$simScore', style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w800, color: Colors.blue)),
                            const Text(' /100', style: TextStyle(fontSize: 10, color: Colors.grey)),
                            if (scoreDelta != 0) ...[
                              const SizedBox(width: 4),
                              Text(
                                scoreDelta > 0 ? '+$scoreDelta' : '$scoreDelta',
                                style: TextStyle(
                                  fontSize: 13,
                                  fontWeight: FontWeight.bold,
                                  color: scoreDelta > 0 ? Colors.red : Colors.green,
                                ),
                              ),
                            ],
                          ],
                        ),
                        const SizedBox(height: 4),
                        RiskLevelBadgeWidget(riskLevel: simulated?['riskLevel'] ?? 'Medium'),
                        const SizedBox(height: 4),
                        PriorityBadgeWidget(priority: simulated?['priority'] ?? 'Medium'),
                        if (simulated?['recommendedResponseWindow'] != null) ...[
                          const SizedBox(height: 4),
                          Text(
                            'Window: ${simulated!['recommendedResponseWindow']}',
                            style: const TextStyle(fontSize: 10, color: Colors.grey),
                          ),
                        ],
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
