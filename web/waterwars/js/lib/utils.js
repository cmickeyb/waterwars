/*
 * Compare two objects.  May fail if objects do not have exactly the same properties
 * This file is part of the Water Wars project itself
 */
function compareObj(obj1, obj2) 
{
    // if obj1 and obj2 are not both objects, return simple comparison
    // If either is null then return a simple comparison, since null objects will have type of Object
    if (typeof obj1 != 'object' || typeof obj2 != 'object' || obj1 === null || obj2 === null) { return (obj1==obj2); }
    // compare indexes in obj1 with those in obj2
    for (index in obj1) {
      var item1 = obj1[index];
      var item2 = obj2[index];
      if (typeof item1 == 'object') if (!compareObj(item1,item2)) return false;
      if (typeof item1 != 'object') if (item1!=item2) return false;
    }
    // if we got this far, the are the same
    return true;
}
