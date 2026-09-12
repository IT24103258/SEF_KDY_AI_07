import unittest
import sys
import os

# Add parent directory to path to import agentic-ai modules
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

from schemas.agent_schemas import ClassificationOutput, PriorityOutput
from validators.deterministic_validator import DeterministicValidator
from tools.domain_tools import ALLOW_LISTED_TOOLS

class TestAgenticAI(unittest.TestCase):
    def test_pydantic_classification_schema_valid(self):
        valid_data = {
            "category": "Electrical Breakdown",
            "subcategory": "Power Outage",
            "confidence_score": 0.95,
            "requires_review": False
        }
        is_valid, parsed, err = DeterministicValidator.validate_schema(valid_data, ClassificationOutput)
        self.assertTrue(is_valid)
        self.assertEqual(parsed.confidence_score, 0.95)

    def test_pydantic_classification_schema_invalid(self):
        invalid_data = {
            "category": "Electrical Breakdown",
            "subcategory": "Power Outage",
            "confidence_score": 1.5 # Invalid: must be <= 1.0
        }
        is_valid, parsed, err = DeterministicValidator.validate_schema(invalid_data, ClassificationOutput)
        self.assertFalse(is_valid)

    def test_allow_listed_tools_exist(self):
        self.assertIn("get_asset_details", ALLOW_LISTED_TOOLS)
        self.assertIn("get_location_details", ALLOW_LISTED_TOOLS)
        self.assertIn("get_sla_config", ALLOW_LISTED_TOOLS)

    def test_human_approval_threshold(self):
        low_conf = {"confidence_score": 0.65}
        req_human, reason = DeterministicValidator.check_human_approval_required("ClassificationAgent", low_conf)
        self.assertTrue(req_human)
        self.assertIn("Low AI classification", reason)

if __name__ == '__main__':
    unittest.main()
