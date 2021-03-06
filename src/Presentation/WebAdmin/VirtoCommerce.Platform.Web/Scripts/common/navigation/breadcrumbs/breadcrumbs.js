﻿angular.module('platformWebApp.breadcrumbs', [
])
.directive('vaBreadcrumb', ['$compile', function ($compile)
{
    return {
    	restrict: 'E',
    	require: 'ngModel',
        replace: true,
        scope: { },
        templateUrl: 'Scripts/common/navigation/breadcrumbs/breadcrumbs.tpl.html',
        link: function (scope, element, attr, ngModelController, linker) {
        	scope.breadcrumbs = {};
        	ngModelController.$render = function () {
        		scope.breadcrumbs = ngModelController.$modelValue;
        	};

        	scope.innerNavigate = function (breadcrumb) {
        		breadcrumb.navigate(breadcrumb);
        	};
        }
    }
}])