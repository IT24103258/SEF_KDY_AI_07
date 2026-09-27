import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/request_provider.dart';
import '../../providers/auth_provider.dart';
import '../../models/request_model.dart';

/// Request Detail screen — /request-detail  (any authenticated role).
///
/// Receives the request [id] as a route argument (String).
/// Shows full detail + classification history.
/// Manager / Administrator users additionally see:
///   • "Run AI Classification" button → POST /api/requests/{id}/classify
///   • "Manual Override" form        → PUT  /api/requests/{id}/classification
class RequestDetailScreen extends StatefulWidget {
  const RequestDetailScreen({super.key});

  @override
  State<RequestDetailScreen> createState() => _RequestDetailScreenState();
}

class _RequestDetailScreenState extends State<RequestDetailScreen> {
  bool _overrideVisible = false;

  // Override form controllers
  final _categoryCtrl      = TextEditingController();
  final _subcategoryCtrl   = TextEditingController();
  final _skillCtrl         = TextEditingController();
  final _reasonCtrl        = TextEditingController();
  final _overrideFormKey   = GlobalKey<FormState>();

  String? _requestId;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    // Route argument is the request ID string, passed from MyRequestsScreen
    final id = ModalRoute.of(context)?.settings.arguments as String?;
    if (id != null && id != _requestId) {
      _requestId = id;
      WidgetsBinding.instance.addPostFrameCallback((_) {
        context.read<RequestProvider>().fetchDetail(id);
      });
    }
  }

  @override
  void dispose() {
    _categoryCtrl.dispose();
    _subcategoryCtrl.dispose();
    _skillCtrl.dispose();
    _reasonCtrl.dispose();
    super.dispose();
  }

  // ── AI classify ──────────────────────────────────────────────────────────
  Future<void> _runClassify() async {
    if (_requestId == null) return;
    final ok = await context.read<RequestProvider>().classifyRequest(_requestId!);
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text(ok ? 'Classification complete.' : 'Classification failed.'),
      backgroundColor: ok ? Colors.green : Colors.red,
    ));
  }

  // ── Manager override ──────────────────────────────────────────────────────
  Future<void> _submitOverride() async {
    if (!_overrideFormKey.currentState!.validate()) return;
    if (_requestId == null) return;

    final ok = await context.read<RequestProvider>().overrideClassification(
      requestId:    _requestId!,
      category:     _categoryCtrl.text.trim(),
      subcategory:  _subcategoryCtrl.text.trim(),
      requiredSkill:_skillCtrl.text.trim(),
      reason:       _reasonCtrl.text.trim(),
    );

    if (!mounted) return;
    if (ok) {
      setState(() => _overrideVisible = false);
      _categoryCtrl.clear();
      _subcategoryCtrl.clear();
      _skillCtrl.clear();
      _reasonCtrl.clear();
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Override recorded.'),
            backgroundColor: Colors.green),
      );
    }
  }

  // ── Helpers ──────────────────────────────────────────────────────────────
  Color _statusColor(String status) {
    switch (status.toLowerCase()) {
      case 'submitted':  return Colors.blue;
      case 'inreview':   return Colors.orange;
      case 'classified': return Colors.purple;
      case 'completed':  return Colors.green;
      case 'cancelled':  return Colors.red;
      default:           return Colors.grey;
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider  = context.watch<RequestProvider>();
    final authProv  = context.watch<AuthProvider>();
    final isManager = ['Manager', 'Administrator']
        .contains(authProv.user?.role ?? '');

    if (provider.isLoading && provider.detail == null) {
      return Scaffold(
        appBar: AppBar(
          title: const Text('Request Detail'),
          backgroundColor: const Color(0xFF2563EB),
          foregroundColor: Colors.white,
        ),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    if (provider.error != null && provider.detail == null) {
      return Scaffold(
        appBar: AppBar(
          title: const Text('Request Detail'),
          backgroundColor: const Color(0xFF2563EB),
          foregroundColor: Colors.white,
        ),
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              Text(provider.error!,
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: Colors.red)),
              const SizedBox(height: 12),
              ElevatedButton(
                onPressed: () => provider.fetchDetail(_requestId ?? ''),
                child: const Text('Retry'),
              ),
            ]),
          ),
        ),
      );
    }

    final req = provider.detail;
    if (req == null) return const SizedBox();

    return Scaffold(
      appBar: AppBar(
        title: Text(req.requestNumber,
            style: const TextStyle(fontFamily: 'monospace', fontSize: 14)),
        backgroundColor: const Color(0xFF2563EB),
        foregroundColor: Colors.white,
        actions: [
          // Edit + Delete: owner-only, only when status is Submitted
          if (!isManager &&
              authProv.user?.id == req.requesterId &&
              req.status.toLowerCase() == 'submitted') ...[
            IconButton(
              icon: const Icon(Icons.edit_outlined),
              tooltip: 'Edit Request',
              onPressed: () => Navigator.pushNamed(
                context,
                '/edit-request',
                arguments: req.id,
              ).then((_) {
                // Refresh detail after returning from editscreen
                if (!context.mounted) return;
                if (_requestId != null) {
                  context.read<RequestProvider>().fetchDetail(_requestId!);
                }
              }),
            ),
            IconButton(
              icon: const Icon(Icons.delete_outline),
              tooltip: 'Delete Request',
              onPressed: () async {
                final confirmed = await showDialog<bool>(
                  context: context,
                  builder: (ctx) => AlertDialog(
                    title: const Text('Delete Request?'),
                    content: const Text('This cannot be undone.'),
                    actions: [
                      TextButton(
                        onPressed: () => Navigator.pop(ctx, false),
                        child: const Text('Cancel'),
                      ),
                      TextButton(
                        onPressed: () => Navigator.pop(ctx, true),
                        child: const Text('Delete',
                            style: TextStyle(color: Colors.red)),
                      ),
                    ],
                  ),
                );
                if (confirmed != true || _requestId == null) return;
                if (!context.mounted) return;
                final ok = await context
                    .read<RequestProvider>()
                    .deleteRequest(_requestId!);
                if (!context.mounted) return;
                if (ok) {
                  Navigator.pop(context);
                } else {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text('Failed to delete request.'),
                      backgroundColor: Colors.red,
                    ),
                  );
                }
              },
            ),
          ],
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async => provider.fetchDetail(req.id),
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [

            // ── Request info card ─────────────────────────────────────────
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(children: [
                      Expanded(
                        child: Text(req.title,
                            style: const TextStyle(
                                fontSize: 17, fontWeight: FontWeight.bold)),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: _statusColor(req.status).withAlpha(30),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Text(req.status,
                            style: TextStyle(
                                fontSize: 12,
                                color: _statusColor(req.status),
                                fontWeight: FontWeight.w600)),
                      ),
                    ]),
                    const Divider(height: 20),
                    _infoRow('Requester', req.requesterName),
                    _infoRow('Location',  req.locationName ?? '—'),
                    _infoRow('Category',  req.categoryName ?? '—'),
                    _infoRow('Asset',     req.assetName    ?? '—'),
                    _infoRow('Submitted',
                        '${req.createdAt.day}/${req.createdAt.month}/${req.createdAt.year}'),
                    const SizedBox(height: 10),
                    const Text('Description',
                        style: TextStyle(
                            fontWeight: FontWeight.w600, color: Colors.grey,
                            fontSize: 12)),
                    const SizedBox(height: 4),
                    Text(req.description,
                        style: const TextStyle(fontSize: 14, height: 1.5)),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 12),

            // ── Manager action panel ──────────────────────────────────────
            if (isManager) ...[
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Classification Actions',
                          style: TextStyle(
                              fontWeight: FontWeight.bold, fontSize: 15)),
                      const SizedBox(height: 12),
                      Row(children: [
                        Expanded(
                          child: ElevatedButton.icon(
                            onPressed: provider.isLoading ? null : _runClassify,
                            icon: const Icon(Icons.auto_awesome, size: 18),
                            label: provider.isLoading
                                ? const SizedBox(
                                    width: 16, height: 16,
                                    child: CircularProgressIndicator(
                                        strokeWidth: 2, color: Colors.white))
                                : const Text('Run AI Classification'),
                            style: ElevatedButton.styleFrom(
                              backgroundColor: const Color(0xFF2563EB),
                              foregroundColor: Colors.white,
                            ),
                          ),
                        ),
                        const SizedBox(width: 8),
                        OutlinedButton.icon(
                          onPressed: () => setState(
                              () => _overrideVisible = !_overrideVisible),
                          icon: const Icon(Icons.edit, size: 18),
                          label: const Text('Override'),
                        ),
                      ]),

                      // Override form (shown/hidden)
                      if (_overrideVisible) ...[
                        const Divider(height: 24),
                        const Text('Manual Override',
                            style: TextStyle(
                                fontWeight: FontWeight.w600, fontSize: 13)),
                        const SizedBox(height: 8),
                        Form(
                          key: _overrideFormKey,
                          child: Column(children: [
                            TextFormField(
                              controller: _categoryCtrl,
                              decoration: const InputDecoration(
                                  labelText: 'Category *',
                                  border: OutlineInputBorder(),
                                  isDense: true),
                              validator: (v) => (v == null || v.trim().isEmpty)
                                  ? 'Category is required.' : null,
                            ),
                            const SizedBox(height: 8),
                            TextFormField(
                              controller: _subcategoryCtrl,
                              decoration: const InputDecoration(
                                  labelText: 'Subcategory (optional)',
                                  border: OutlineInputBorder(),
                                  isDense: true),
                            ),
                            const SizedBox(height: 8),
                            TextFormField(
                              controller: _skillCtrl,
                              decoration: const InputDecoration(
                                  labelText: 'Required Skill (optional)',
                                  border: OutlineInputBorder(),
                                  isDense: true),
                            ),
                            const SizedBox(height: 8),
                            TextFormField(
                              controller: _reasonCtrl,
                              decoration: const InputDecoration(
                                  labelText: 'Reason (optional)',
                                  border: OutlineInputBorder(),
                                  isDense: true),
                            ),
                            const SizedBox(height: 10),
                            Row(children: [
                              Expanded(
                                child: ElevatedButton(
                                  onPressed: provider.isLoading
                                      ? null : _submitOverride,
                                  style: ElevatedButton.styleFrom(
                                    backgroundColor: Colors.orange.shade700,
                                    foregroundColor: Colors.white,
                                  ),
                                  child: const Text('Save Override'),
                                ),
                              ),
                              const SizedBox(width: 8),
                              TextButton(
                                onPressed: () => setState(
                                    () => _overrideVisible = false),
                                child: const Text('Cancel'),
                              ),
                            ]),
                          ]),
                        ),
                        if (provider.error != null)
                          Padding(
                            padding: const EdgeInsets.only(top: 8),
                            child: Text(provider.error!,
                                style: const TextStyle(color: Colors.red,
                                    fontSize: 12)),
                          ),
                      ],
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 12),
            ],

            // ── Classification history ────────────────────────────────────
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Classification History',
                        style: TextStyle(
                            fontWeight: FontWeight.bold, fontSize: 15)),
                    const SizedBox(height: 8),
                    if (req.classifications.isEmpty)
                      const Padding(
                        padding: EdgeInsets.symmetric(vertical: 12),
                        child: Text('No classifications yet.',
                            style: TextStyle(color: Colors.grey)),
                      )
                    else
                      ...req.classifications.asMap().entries.map((entry) {
                        final i = entry.key;
                        final c = entry.value;
                        return _classificationTile(c, isLast:
                            i == req.classifications.length - 1);
                      }),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _infoRow(String label, String value) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 3),
        child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          SizedBox(
            width: 90,
            child: Text(label,
                style: const TextStyle(
                    color: Colors.grey, fontSize: 12,
                    fontWeight: FontWeight.w500)),
          ),
          Expanded(
            child: Text(value,
                style: const TextStyle(fontSize: 13)),
          ),
        ]),
      );

  Widget _classificationTile(ClassificationResult c, {bool isLast = false}) {
    final pct = (c.confidenceScore * 100).toStringAsFixed(0);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: c.isOverride
                ? Colors.orange.shade50
                : Colors.blue.shade50,
            border: Border.all(
                color: c.isOverride
                    ? Colors.orange.shade200
                    : Colors.blue.shade100),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(children: [
                Expanded(
                  child: Text(
                    c.subcategory != null
                        ? '${c.category} / ${c.subcategory}'
                        : c.category,
                    style: const TextStyle(
                        fontWeight: FontWeight.bold, fontSize: 14),
                  ),
                ),
                if (c.isOverride)
                  Container(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 6, vertical: 2),
                    decoration: BoxDecoration(
                      color: Colors.orange.shade100,
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: const Text('OVERRIDE',
                        style: TextStyle(
                            fontSize: 10,
                            color: Colors.deepOrange,
                            fontWeight: FontWeight.bold)),
                  ),
              ]),
              const SizedBox(height: 6),
              Wrap(spacing: 16, children: [
                _chip('Confidence', '$pct%'),
                _chip('Review', c.requiresReview ? 'Yes' : 'No'),
                if (c.requiredSkill != null)
                  _chip('Skill', c.requiredSkill!),
                if (c.overriddenByUserName != null)
                  _chip('By', c.overriddenByUserName!),
              ]),
              if (c.reason != null && c.reason!.isNotEmpty) ...[
                const SizedBox(height: 6),
                Text('"${c.reason}"',
                    style: const TextStyle(
                        fontSize: 12,
                        fontStyle: FontStyle.italic,
                        color: Colors.grey)),
              ],
              const SizedBox(height: 4),
              Text(
                '${c.createdAt.day}/${c.createdAt.month}/${c.createdAt.year} '
                '${c.createdAt.hour.toString().padLeft(2, '0')}:'
                '${c.createdAt.minute.toString().padLeft(2, '0')}',
                style: const TextStyle(fontSize: 11, color: Colors.grey),
              ),
            ],
          ),
        ),
        if (!isLast) const SizedBox(height: 8),
      ],
    );
  }

  Widget _chip(String label, String value) => Text(
        '$label: ',
        style: const TextStyle(fontSize: 12, color: Colors.grey),
      ).toRichText(label, value);
}

// Tiny helper extension to avoid repeating TextStyle combos
extension _RichLabel on Text {
  RichText toRichText(String label, String value) => RichText(
        text: TextSpan(
          style: const TextStyle(fontSize: 12, color: Colors.grey),
          children: [
            TextSpan(text: '$label: '),
            TextSpan(
                text: value,
                style: const TextStyle(
                    color: Colors.black87, fontWeight: FontWeight.w600)),
          ],
        ),
      );
}
